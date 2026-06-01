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
    }

    private ISkillProvider? _provider;
    private Thread? _skillThread;
    private CancellationTokenSource? _skillsCTS;

    [MethodCallBinding("canUseSkill")]
    private bool _canUseSkill(int index)
    {
        return false;
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
    
    public int SkillTimeout { get; set; } = -1;

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
        SkillTimeout = skill.SkillTimeout;
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
                SkillTimeout = skill.SkillTimeout;
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
            if (Combat.StopAttacking)
            {
                _counterAttack.WaitOne(10000);
                Combat.StopAttacking = false;
            }

            if (!Player.Alive)
            {
                _provider?.OnPlayerDeath();
            }

            // target reset if player has no target
            _provider?.OnTargetReset();

            // if the player has target or bot attack without target option is on
            // then activate the skills
            if ((Options.AttackWithoutTarget && Player is { Loaded: true, Playing: true }) || Player is { HasTarget: true, Loaded: true, Playing: true })
            {
                _Poll(token);
            }

            // wait for skill interval
            if (!token.IsCancellationRequested)
                await Task.Delay(SkillInterval, token);
        }
    }

    private void _Poll(CancellationToken token)
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
            return;

        (int index, int skillS) = _provider!.GetNextSkill();

        if (index == -1 || skillS == -1)
            return;

        switch (_provider?.ShouldUseSkill(index, CanUseSkill(skillS)))
        {
            case true:
                if (_playerSkills is not null && skillS < _playerSkills.Length && !_playerSkills[skillS].IsOk)
                {
                    break;
                }
                UseSkill(skillS);
                break;

            case null:
                (int indexS, int skill) = _provider!.GetNextSkill();
                if (indexS == -1 || skill == -1)
                    break;
                if (_playerSkills is not null && skill < _playerSkills.Length && !_playerSkills[skill].IsOk)
                {
                    break;
                }
                UseSkill(skill);
                break;

            default:
                break;
        }

        return;

        // This method will activate a skill
        void UseSkill(int skill)
        {
            switch (SkillUseMode)
            {
                // if SkillUseMode is UseIfAvailable then use skills without waiting for cooldown
                case SkillUseMode.UseIfAvailable:
                    if ((Options.AttackWithoutTarget || CanUseSkill(skill)) && !Combat.StopAttacking)
                        this.UseSkill(skill);
                    break;

                // if SkillUseMode is WaitForCooldown then use skills with waiting for cooldown
                case SkillUseMode.WaitForCooldown:
                    if (Options.AttackWithoutTarget || (skill != -1 && Wait.ForTrue(() => CanUseSkill(skill), null, SkillTimeout, SkillInterval) && !Combat.StopAttacking))
                        this.UseSkill(skill);
                    break;
            }
        }
    }

    private readonly AutoResetEvent _counterAttack = new(false);

    private void CounterAttack(ScriptSkill recipient, CounterAttackMessage message)
    {
        if (message.Faded)
            recipient._counterAttack.Set();
    }
    private AdvancedSkillProvider CreateAdvancedSkillProvider()
    {
        return new AdvancedSkillProvider(Player, Self, Target, Combat, Flash)
        {
            ResetOnTarget = ResetComboOnTargetChange
        };
    }

}


