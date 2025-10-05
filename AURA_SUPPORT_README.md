# Advanced Skills - Aura Support

## Overview

The Advanced Skills system now supports aura-based conditions, allowing you to control skill usage based on buff/debuff auras on yourself or your target.

## Syntax

### Self Aura Rules
Check your own character's auras:
- `A:AuraName>value` - Use skill only if aura value is **greater than** the specified value
- `A:AuraName<value` - Use skill only if aura value is **less than** the specified value  
- `A:AuraName=value` - Use skill only if aura value **equals** the specified value

### Target Aura Rules
Check your target's auras:
- `TA:AuraName>value` - Use skill only if target's aura value is **greater than** the specified value
- `TA:AuraName<value` - Use skill only if target's aura value is **less than** the specified value
- `TA:AuraName=value` - Use skill only if target's aura value **equals** the specified value

## Examples

### Basic Usage
```
1 | 2 A:Damage>5 | 3 | 4
```
- Skill 2 will only be used when you have a "Damage" aura with value greater than 5

### Target Aura Check
```
1 | 2 TA:Weakness=1 | 3 | 4
```
- Skill 2 will only be used when your target has a "Weakness" aura with value equal to 1

### Combined with Health/Mana
```
1 | 2 H>50 A:PowerUp>3 | 3 M<80 | 4
```
- Skill 2 requires BOTH: Health > 50% AND "PowerUp" aura > 3
- Skill 3 requires: Mana < 80%

### Multiple Aura Checks
```
1 A:Buff1>0 | 2 TA:Debuff<5 | 3 | 4
```
- Skill 1 checks for your "Buff1" aura
- Skill 2 checks for target's "Debuff" aura

### With Skip Rule
```
1 | 2 A:Combo=3 S | 3 | 4
```
- Skill 2 will only be used when "Combo" aura equals 3
- If the condition is not met OR skill is on cooldown, it will skip to the next skill

### Complex Example
```
1 | 2 H>70 A:Stack>2 W100 | 3 TA:Defense<10 S | 4 M>50
```
- Skill 2: Health > 70% AND "Stack" aura > 2, then wait 100ms
- Skill 3: Target's "Defense" aura < 10, skip if not available
- Skill 4: Mana > 50%

## Important Notes

1. **Aura Names**: Case-insensitive (e.g., `A:damage` is the same as `A:Damage`)
2. **Missing Auras**: If an aura doesn't exist, the condition fails and the skill won't be used
3. **Aura Values**: Must be numeric integers
4. **Combining Rules**: All conditions in a skill rule must be true for the skill to execute
5. **Compatibility**: Works with all existing rule types (Health, Mana, Wait, Skip)

## Use Cases

### Stack Management
```
1 | 2 A:Stacks<5 | 3 A:Stacks=5 | 4
```
Build stacks with skill 2 until you have 5, then use skill 3

### Debuff Checking
```
1 | 2 TA:Resistance>0 | 3 TA:Resistance=0 | 4
```
Use different skills based on whether target has resistance

### Buff Maintenance
```
1 A:AttackBoost<1 | 2 | 3 | 4
```
Only use skill 1 (buff skill) when the buff is missing or expired

### Combo System
```
1 | 2 A:Combo=1 | 3 A:Combo=2 | 4 A:Combo=3
```
Progress through a combo chain based on combo aura value

## Troubleshooting

**Skill not working?**
- Verify aura name is spelled correctly
- Check if aura actually exists in game
- Ensure aura value is numeric
- Confirm you're using the right prefix (A: for self, TA: for target)

**Need to debug?**
- Test with simple rules first
- Use the skill display to verify parsing
- Check that all required conditions are being met
