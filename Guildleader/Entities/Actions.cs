using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Guildleader.Entities
{
    //handles any and all generic character actions, like moves, digging, crafting, etc
    public class EntityAction
    {
        readonly Entity caster;
        readonly float startupTime, sustain, endLag; //the attack, sustain, and release of actions. time until its effects are initiated, time spent on sustained actions, and release time, in that order
        float EndSustainTime { get { return startupTime + sustain; } }
        float TotalActionTime { get { return startupTime + sustain + endLag; } }

        float currentTimeIntoAction;

        public Action<Entity> beforeStartupAction, postStartupAction, sustainAction, postSustainAction, postEndAction;

        CurrentPhase phase;

        enum CurrentPhase
        {
            notYetStarted,
            performingStartup,
            performingSustain,
            performingEnd,
            finished
        }

        public EntityAction(Entity user, float startup, float sus, float end)
        {
            caster = user;
            startupTime = startup; sustain = sus; endLag = end;
        }

        public void UpdateAction(float timestep)
        {
            if (phase == CurrentPhase.notYetStarted)
            {
                phase = CurrentPhase.performingStartup;
                beforeStartupAction?.Invoke(caster);
            }
            if (phase == CurrentPhase.performingStartup && currentTimeIntoAction >= startupTime)
            {
                phase = CurrentPhase.performingSustain;
                postStartupAction?.Invoke(caster);
            }
            if (phase == CurrentPhase.performingSustain && currentTimeIntoAction >= EndSustainTime)
            {
                phase = CurrentPhase.performingEnd;
                postSustainAction?.Invoke(caster);
            }
            if (phase == CurrentPhase.performingEnd && currentTimeIntoAction >= TotalActionTime)
            {
                phase = CurrentPhase.finished;
                postEndAction?.Invoke(caster);
            }
            currentTimeIntoAction += timestep;
        }
    }

    public class ActionDetails
    {
        public float xyRotation, zRotation; //in degrees
    }
}
