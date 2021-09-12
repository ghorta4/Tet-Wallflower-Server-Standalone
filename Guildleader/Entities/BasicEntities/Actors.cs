using System;
using System.Collections.Generic;
using System.Text;

namespace Guildleader.Entities.BasicEntities
{
    public class Actors : PhysicalObject //these are objects that can take actions, perform tasks, etc. 
    {
        EntityAction primaryAction, secondaryAction; //some actions can be primary and secondary; some others can only be primary (needs focus).

        public override string GetSpriteName()
        {
            throw new NotImplementedException();
        }

        public override void Update(float deltaTime, byte lastUpdatedFrameNumber)
        {
            primaryAction?.UpdateAction(deltaTime);
            secondaryAction?.UpdateAction(deltaTime);
            base.Update(deltaTime, lastUpdatedFrameNumber);
        }

        public void SetEntityAction (EntityAction action, bool canBeSecondaryAction)
        {
            if (canBeSecondaryAction && secondaryAction == null)
            {
                secondaryAction = action;
                return;
            }
            if (primaryAction.canBeSecondaryAction && secondaryAction == null)
            {
                secondaryAction = primaryAction;
                primaryAction = action;
                return;
            }
            primaryAction = action;
        }
    }
}
