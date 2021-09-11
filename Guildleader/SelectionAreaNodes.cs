using System;
using System.Collections.Generic;
using System.Text;

namespace Guildleader
{
    public abstract class FFXNode
    {
        public float timeAlive, totalLifespan;
        public int r = 255, g = 255, b = 255, a = 255;
        public Int4 Color { get { return new Int4(r, g, b, a); } }
        public abstract byte[] ConvertToBytes();
        public abstract NodeKey ThisNodeKey();


        //functions for drawing different nodes
        public enum NodeKey
        {
            ExpandingShell,
            Smear,
            RectangularPrism
        }
    }

    public class ExpandingShellNode : FFXNode
    {
        public Int3 center = Int3.Zero;
        public float maxSize, shellThickness;

        public override NodeKey ThisNodeKey()
        {
            return NodeKey.ExpandingShell;
        }

        public override byte[] ConvertToBytes()
        {
            throw new NotImplementedException();
        }
    }

    public class SmearNode : FFXNode
    {
        public float x, y, z, angleXY, angleZ;
        public float travelDistance;
        public float stretch = 1;
        public override NodeKey ThisNodeKey()
        {
            return NodeKey.Smear;
        }
        public override byte[] ConvertToBytes()
        {
            throw new NotImplementedException();
        }
    }

    public class RectPrismNode : FFXNode
    {
        public Int3 corner, size;

        public override NodeKey ThisNodeKey()
        {
            return NodeKey.RectangularPrism;
        }
        public override byte[] ConvertToBytes()
        {
            throw new NotImplementedException();
        }
    }
}
