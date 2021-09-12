using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Guildleader.Entities
{
    //handles any and all generic character actions, like moves, digging, crafting, etc
    public class EntityAction
    {
        public readonly Entity caster;
        readonly float startupTime, sustain, endLag; //the attack, sustain, and release of actions. time until its effects are initiated, time spent on sustained actions, and release time, in that order
        float EndSustainTime { get { return startupTime + sustain; } }
        float TotalActionTime { get { return startupTime + sustain + endLag; } }

        float currentTimeIntoAction;

        public bool canBeSecondaryAction;

        public Action<EntityAction> beforeStartupAction, postStartupAction, sustainAction, postSustainAction, postEndAction;

        public ActionDetails details = new ActionDetails();

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
                beforeStartupAction?.Invoke(this);
            }
            if (phase == CurrentPhase.performingStartup && currentTimeIntoAction >= startupTime)
            {
                phase = CurrentPhase.performingSustain;
                postStartupAction?.Invoke(this);
            }
            if (phase == CurrentPhase.performingSustain && currentTimeIntoAction >= EndSustainTime)
            {
                phase = CurrentPhase.performingEnd;
                postSustainAction?.Invoke(this);
            }
            if (phase == CurrentPhase.performingEnd && currentTimeIntoAction >= TotalActionTime)
            {
                phase = CurrentPhase.finished;
                postEndAction?.Invoke(this);
            }
            currentTimeIntoAction += timestep;
        }
    }

    public class ActionDetails
    {
        public float xyRotation, zRotation; //in degrees for direction aim
        public Int3 targetCoords; //for positional select
    }
}
