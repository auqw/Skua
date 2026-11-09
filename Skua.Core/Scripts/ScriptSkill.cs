using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Flash;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Models.Skills;
using Skua.Core.Skills;

namespace Skua.Core.Scripts;

public partial class ScriptSkill : IScriptSkill
{
    private const string genericSkills = "0 | 1 | 2 | 3 | 4";
    private readonly Lazy<IFlashUtil> _lazyFlash;
    private readonly Lazy<IScriptPlayer> _lazyPlayer;
    private readonly Lazy<IScriptSelfAuras> _lazySelf;
    private readonly Lazy<IScriptTargetAuras> _lazyTarget;
    private readonly Lazy<IScriptCombat> _lazyCombat;
    private readonly Lazy<IScriptWait> _lazyWait;
    private readonly Lazy<IScriptOption> _lazyOptions;
    private readonly Lazy<IScriptInventory> _lazyInventory;

    private IFlashUtil Flash => _lazyFlash.Value;
    private IScriptPlayer Player => _lazyPlayer.Value;
    private IScriptSelfAuras Self => _lazySelf.Value;
    private IScriptTargetAuras Target => _lazyTarget.Value;
    private IScriptCombat Combat => _lazyCombat.Value;
    private IScriptWait Wait => _lazyWait.Value;
    private IScriptOption Options => _lazyOptions.Value;
    private IScriptInventory Inventory => _lazyInventory.Value;

    public IAdvancedSkillContainer AdvancedSkillContainer { get; set; }

    public ScriptSkill(
        IAdvancedSkillContainer advContainer,
        Lazy<IFlashUtil> flash,
        Lazy<IScriptPlayer> player,
        Lazy<IScriptSelfAuras> self,
        Lazy<IScriptTargetAuras> target,
        Lazy<IScriptCombat> combat,
        Lazy<IScriptOption> options,
        Lazy<IScriptInventory> inventory,
        Lazy<IScriptWait> wait)
    {
        _lazyFlash = flash;
        _lazyPlayer = player;
        _lazySelf = self;
        _lazyTarget = target;
        _lazyCombat = combat;
        _lazyWait = wait;
        _lazyOptions = options;
        _lazyInventory = inventory;
        AdvancedSkillContainer = advContainer;

        IMessenger messenger = StrongReferenceMessenger.Default;
        messenger.Register<ScriptSkill, CounterAttackMessage, int>(this, (int)MessageChannels.GameEvents, CounterAttack);
    }

    private ISkillProvider? _provider;
    private Thread? _skillThread;
    private CancellationTokenSource? _skillsCTS;

    [MethodCallBinding("canUseSkill")]
    private bool _canUseSkill(int index)
    {
        return false;
    }

    [MethodCallBinding("skillCooldownRemaining")]
    private int _skillCooldownRemaining(int index)
    {
        return 0;
    }

    [MethodCallBinding("globalCooldownRemaining")]
    private int _globalCooldownRemaining()
    {
        return 0;
    }

    [MethodCallBinding("useSkill")]
    private bool _useSkill(int index)
    {
        return false;
    }

    public ISkillProvider? OverrideProvider { get; set; } = null;
    public ISkillProvider BaseProvider { get; private set; }
    public bool TimerRunning { get; private set; } = false;
    public bool IsPaused { get; private set; } = false;
    public int SkillInterval { get; set; } = 100;
    
    private int _skillTimeoutMs = -1;

    // Set when the timeout was given in intervals (SkillTimeout, StartAdvanced(skills, timeout, mode)): it then stays a
    // count of intervals and follows SkillInterval at the time of the wait, as it always did.
    private int? _skillTimeoutIntervals;

    public int SkillTimeoutMs
    {
        get => _skillTimeoutIntervals is int intervals
            ? (intervals <= 0 ? intervals : (int)Math.Min(int.MaxValue, (long)intervals * Math.Max(1, SkillInterval)))
            : _skillTimeoutMs;
        set
        {
            _skillTimeoutMs = value;
            _skillTimeoutIntervals = null;
        }
    }

    public int SkillTimeout
    {
        get => _skillTimeoutIntervals
            ?? (_skillTimeoutMs <= 0 ? _skillTimeoutMs : (int)(((long)_skillTimeoutMs + Math.Max(1, SkillInterval) - 1) / Math.Max(1, SkillInterval)));
        set => _skillTimeoutIntervals = value;
    }

    public bool ResetComboOnTargetChange { get; set; } = false;

    public SkillUseMode SkillUseMode { get; set; } = SkillUseMode.UseIfAvailable;
    private ManualResetEvent _pauseEvent = new(true);


    public void Start()
    {
        if (BaseProvider is null)
        {
            BaseProvider = CreateAdvancedSkillProvider();
            BaseProvider.Load(genericSkills);
            _provider = BaseProvider;
        }
        _provider = OverrideProvider ?? BaseProvider;

        if (TimerRunning)
            return;

        _skillThread = new(async () =>
        {
            _skillsCTS = new();
            try
            {
                await _Timer(_skillsCTS.Token);
            }
            catch { }
            finally
            {
                _skillsCTS?.Dispose();
                _skillsCTS = null;
                TimerRunning = false;
            }
        })
        {
            Name = "Skill Timer"
        };
        _skillThread.Start();
        TimerRunning = true;
    }

    public void Stop()
    {
        _provider?.Stop();
        _skillsCTS?.Cancel();
        Wait.ForTrue(() => !TimerRunning, 2);
    }

    public void Pause()
    {
        if (!TimerRunning || IsPaused)
            return;

        IsPaused = true;
        _pauseEvent.Reset();
    }

    public void Resume()
    {
        if (!TimerRunning || !IsPaused)
            return;

        IsPaused = false;
        _pauseEvent.Set();
    }

    public void SetProvider(ISkillProvider provider)
    {
        _provider = OverrideProvider = provider;
    }

    public void UseBaseProvider()
    {
        _provider = BaseProvider;
    }

    public void LoadAdvanced(string className, bool autoEquip, ClassUseMode useMode = ClassUseMode.Base)
    {
        if (className == "generic")
        {
            ResetComboOnTargetChange = false;
            OverrideProvider = CreateAdvancedSkillProvider();
            OverrideProvider.Load(genericSkills);
            SkillUseMode = SkillUseMode.UseIfAvailable;
            return;
        }

        if (autoEquip)
        {
            Inventory.EquipItem(className);
            Wait.ForItemEquip(className);
        }

        List<AdvancedSkill> skills = new();
        foreach (AdvancedSkill s in AdvancedSkillContainer.LoadedSkills)
        {
            if (string.Equals(s.ClassName, className, StringComparison.CurrentCultureIgnoreCase))
                skills.Add(s);
        }

        if (skills.Count == 0)
        {
            ResetComboOnTargetChange = false;
            OverrideProvider = CreateAdvancedSkillProvider();
            OverrideProvider.Load(genericSkills);
            SkillUseMode = SkillUseMode.UseIfAvailable;
            return;
        }

        AdvancedSkill skill = skills.Find(s => s.ClassUseMode == useMode) ?? skills.FirstOrDefault()!;

        ResetComboOnTargetChange = skill.ResetComboOnTargetChange;

        OverrideProvider = CreateAdvancedSkillProvider();
        OverrideProvider.Load(skill.Skills);
        SkillTimeoutMs = skill.SkillTimeoutMs;
        SkillUseMode = skill.SkillUseMode;
    }

    public void LoadAdvanced(string skills, int skillTimeout = -1, SkillUseMode skillMode = SkillUseMode.UseIfAvailable)
    {
        ResetComboOnTargetChange = false;
        OverrideProvider = CreateAdvancedSkillProvider();
        SkillTimeout = skillTimeout;
        SkillUseMode = skillMode;
        OverrideProvider.Load(skills);
    }

    public void LoadAdvanced(string className, string mode, bool autoEquip = true)
    {
        if (Enum.TryParse<ClassUseMode>(mode, ignoreCase: true, out ClassUseMode classMode))
        {
            LoadAdvanced(className, autoEquip, classMode);
        }
        else
        {
            AdvancedSkill? skill = AdvancedSkillContainer.GetClassModeSkills(className, mode);
            if (skill != null)
            {
                if (autoEquip)
                {
                    Inventory.EquipItem(className);
                    Wait.ForItemEquip(className);
                }

                ResetComboOnTargetChange = skill.ResetComboOnTargetChange;

                OverrideProvider = CreateAdvancedSkillProvider();
                OverrideProvider.Load(skill.Skills);
                SkillTimeoutMs = skill.SkillTimeoutMs;
                SkillUseMode = skill.SkillUseMode;
            }
            else
            {
                ResetComboOnTargetChange = false;
                OverrideProvider = CreateAdvancedSkillProvider();
                OverrideProvider.Load(genericSkills);
                SkillUseMode = SkillUseMode.UseIfAvailable;
            }
        }
    }

    private int _lastRank = -1;
    private SkillInfo[] _playerSkills = null!;

    private async Task _Timer(CancellationToken token)
    {
        SkillInfo[]? playerSkills = Player.Skills;
        if (playerSkills is not null && playerSkills.Length > 0)
            _playerSkills = playerSkills;

        while (!token.IsCancellationRequested)
        {
            _pauseEvent.WaitOne();
            int delay = SkillInterval;
            try
            {
                if (Combat.StopAttacking)
                {
                    if (WaitForCounterAttack(token))
                        Combat.StopAttacking = false;
                }

                if (!Player.Alive)
                {
                    _provider?.OnPlayerDeath();
                }

                // target reset if player has no target
                if (_provider?.OnTargetReset() == true)
                    continue;

                // if the player has target or bot attack without target option is on
                // then activate the skills
                if ((Options.AttackWithoutTarget && Player is { Loaded: true, Playing: true }) || Player is { HasTarget: true, Loaded: true, Playing: true })
                {
                    // No skill can be used while the global cooldown runs, and a turn taken then would only skip the
                    // next skill: take turns only when it is over, and sleep until it ends (at most SkillInterval at a time).
                    int globalCooldown = GlobalCooldownRemaining();
                    if (globalCooldown <= 0)
                    {
                        delay = _Poll(token);
                        globalCooldown = GlobalCooldownRemaining();
                    }

                    if (globalCooldown > 0)
                        delay = Math.Min(globalCooldown, SkillInterval);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // One failed turn (game data missing for a moment, say) must not end the skill timer for good.
                Trace.WriteLine($"Skill timer: {e.GetType().Name}: {e.Message}");
            }

            if (!token.IsCancellationRequested)
                await Task.Delay(delay, token);
        }
    }

    /// <summary>
    /// Takes one turn of the rotation.
    /// </summary>
    /// <returns>How long to sleep before the next turn, in milliseconds, if no global cooldown is running.</returns>
    private int _Poll(CancellationToken token)
    {
        // if the current player has skills and the current class rank is different from the last rank
        // then update the skills since classes will enable a certain skill base on the rank
        // Also refresh if _playerSkills is null
        if (Player.CurrentClassRank > _lastRank || _playerSkills is null)
        {
            // Update the player skills skills
            SkillInfo[]? playerSkills = Player.Skills;
            if (playerSkills is not null && playerSkills.Length > 0)
                _playerSkills = playerSkills;

            if (_playerSkills is not null)
            {
                int k = 0;
                using FlashArray<object> skills = (FlashArray<object>)Flash.CreateFlashObject<object>("world.actions.active").ToArray();
                foreach (FlashObject<object> skill in skills)
                {
                    if (k >= _playerSkills.Length)
                        break;
                    using FlashObject<long> ts = (FlashObject<long>)skill.GetChild<long>("ts");
                    ts.Value = _playerSkills[k++]?.LastUse ?? 0;
                }
            }
        }

        // Store the last rank if the player is ranking up
        _lastRank = Player.CurrentClassRank;
        if (token.IsCancellationRequested)
            return SkillInterval;

        // Use if Available goes through the steps in order and uses the first one that can be used now: a step whose
        // skill cannot be used is passed over at once rather than costing a whole SkillInterval. After a round with
        // nothing used the rotation is back where it started, and the timer sleeps until the soonest of them is ready.
        bool tryEachStep = SkillUseMode == SkillUseMode.UseIfAvailable;
        int stepCount = Math.Max(1, _provider!.SkillCount);
        int steps = tryEachStep ? stepCount : 1;
        int soonest = int.MaxValue;
        for (int step = 0; step < steps && !token.IsCancellationRequested; step++)
        {
            // A new target restarts the rotation (when set to): stop here, and the next turn starts from the top.
            if (step > 0 && _provider?.OnTargetReset() == true)
                return 1;

            (int index, int skillS) = _provider!.GetNextSkill();

            if (index == -1 || skillS == -1)
                return SkillInterval;

            bool canUse = Usable(skillS);
            if (tryEachStep && !canUse)
            {
                int left = SkillCooldownRemaining(skillS);
                if (left > 0)
                    soonest = Math.Min(soonest, left);
                continue;
            }

            switch (_provider?.ShouldUseSkill(index, canUse))
            {
                case true:
                    if (_playerSkills is not null && skillS < _playerSkills.Length && !_playerSkills[skillS].IsOk)
                    {
                        break;
                    }
                    UseSkill(skillS);
                    return SkillInterval;

                case null:
                    // Skipped ("Skip if not available"): the next step gets its turn now, with its own rules checked,
                    // rather than after a SkillInterval. At most one round, so a rotation of skipped steps still sleeps.
                    if (steps < stepCount)
                        steps++;
                    break;

                default:
                    break;
            }
        }

        return soonest == int.MaxValue ? SkillInterval : Math.Min(soonest, SkillInterval);

        // This method will activate a skill
        void UseSkill(int skill)
        {
            switch (SkillUseMode)
            {
                // if SkillUseMode is UseIfAvailable then use skills without waiting for cooldown
                case SkillUseMode.UseIfAvailable:
                    if (Usable(skill) && !Combat.StopAttacking)
                        this.UseSkill(skill);
                    break;

                // if SkillUseMode is WaitForCooldown then use skills with waiting for cooldown
                case SkillUseMode.WaitForCooldown:
                    if (skill == -1)
                        break;

                    bool canUseAfterWait = WaitUntilUsable(skill, token, out bool targetReset);

                    if (targetReset)
                        return;

                    if (canUseAfterWait && !Combat.StopAttacking)
                        this.UseSkill(skill);

                    break;
            }
        }
    }

    /// <summary>
    /// Whether the skill can be used now. With <see cref="IScriptOption.AttackWithoutTarget"/> the target is not checked (the game picks one in range
    /// itself), but everything else the game would refuse is: cooldowns, mana, the potion slot, stuns, locked skills.
    /// </summary>
    private bool Usable(int skill) => Options.AttackWithoutTarget ? Flash.Call<bool>("canUseSkill", skill, true) : CanUseSkill(skill);

    /// <summary>
    /// Waits for the skill's cooldown to end, for up to <see cref="SkillTimeoutMs"/> (no limit when not above 0).
    /// It sleeps for the time left rather than a whole <see cref="SkillInterval"/>, so the skill is used as soon as it is ready,
    /// but never longer than <see cref="SkillInterval"/> at a time, so a target change is still noticed.
    /// A skill held up by anything else (mana, a stun, an empty potion slot, a locked skill, no target) is not waited for: it would
    /// not clear by waiting, or not reliably, so the rotation moves on, as it did when the game refused the skill.
    /// </summary>
    /// <returns><see langword="true"/> if the skill can be used.</returns>
    private bool WaitUntilUsable(int skill, CancellationToken token, out bool targetReset)
    {
        targetReset = false;
        long deadline = SkillTimeoutMs > 0 ? Environment.TickCount64 + SkillTimeoutMs : long.MaxValue;
        while (!token.IsCancellationRequested)
        {
            if (_provider?.OnTargetReset() == true)
            {
                targetReset = true;
                return false;
            }

            if (Usable(skill))
                return true;

            int cooldown = SkillCooldownRemaining(skill);
            if (cooldown <= 0)
                return Usable(skill); // its cooldown may have ended between the two reads

            long left = deadline - Environment.TickCount64;
            if (left <= 0)
                return false;

            token.WaitHandle.WaitOne((int)Math.Max(1, Math.Min(Math.Min(cooldown, SkillInterval), left)));
        }

        return false;
    }

    // Longest wait when attacks were stopped by something other than a counter attack (a script setting StopAttacking).
    private const int StopAttackingFallbackMs = 10000;
    // Safety net during a counter attack: ScriptInterface ends every counter itself (removal, death or expiry).
    private const int CounterAttackSafetyMs = 30000;

    private volatile bool _counterAttackActive;
    private long _counterAttackFades;

    private void CounterAttack(ScriptSkill recipient, CounterAttackMessage message)
    {
        recipient._counterAttackActive = !message.Faded;
        if (message.Faded)
            Interlocked.Increment(ref recipient._counterAttackFades);
    }

    /// <summary>
    /// Waits while attacks are stopped: until a counter attack ends, something else allows attacks again (player death,
    /// script stop, <c>UltraBossHelper.SetAttacksStopped(false)</c>), or the wait runs out. Only a counter end that
    /// happens during this wait counts, so an old one can't cut short a later pause.
    /// </summary>
    /// <returns>Whether the caller should allow attacks again; not while a new counter attack is on.</returns>
    private bool WaitForCounterAttack(CancellationToken token)
    {
        long fades = Interlocked.Read(ref _counterAttackFades);
        Stopwatch waited = Stopwatch.StartNew();
        while (!token.IsCancellationRequested && Combat.StopAttacking && Interlocked.Read(ref _counterAttackFades) == fades)
        {
            if (waited.ElapsedMilliseconds >= (_counterAttackActive ? CounterAttackSafetyMs : StopAttackingFallbackMs))
                return true;
            token.WaitHandle.WaitOne(50);
        }
        return !_counterAttackActive;
    }
    private AdvancedSkillProvider CreateAdvancedSkillProvider()
    {
        return new AdvancedSkillProvider(Player, Self, Target, Combat, Flash)
        {
            ResetOnTarget = ResetComboOnTargetChange
        };
    }

}


