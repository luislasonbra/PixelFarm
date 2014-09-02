//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# Port port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software 
// is granted provided this copyright notice appears in all copies. 
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------
using System.Collections.Generic;

namespace MatterHackers.Agg.VertexSource
{
    // in the original agg this was conv_transform
    public interface IVertextTransform
    { 

    }

    public class VertexSourceApplyTransform : IVertextTransform, IVertexSource
    {
        readonly IVertexSource vtxsrc;
        readonly Transform.ITransform transformToApply;

        public VertexSourceApplyTransform(IVertexSource vertexSource, Transform.ITransform newTransformeToApply)
        {
            vtxsrc = vertexSource;
            transformToApply = newTransformeToApply;
        }

        public bool IsDynamicVertexGen
        {
            get
            {
                return vtxsrc.IsDynamicVertexGen;
            }

        }
        public IEnumerable<VertexData> GetVertexIter()
        {
            //transform 'on-the-fly' 
            foreach (VertexData vertexData in vtxsrc.GetVertexIter())
            {
                VertexData transformedVertex = vertexData;
                if (ShapePath.IsVertextCommand(transformedVertex.command))
                {
                    //transform 2d
                    transformToApply.Transform(ref transformedVertex.position.x, ref transformedVertex.position.y);
                }
                yield return transformedVertex;
            }
        }

        public void DoTransform(List<VertexData> output)
        {

            foreach (VertexData vx in vtxsrc.GetVertexIter())
            {
                VertexData transformedVertex = vx;
                switch (transformedVertex.command)
                {
                    case ShapePath.FlagsAndCommand.CommandMoveTo:
                    case ShapePath.FlagsAndCommand.CommandLineTo:
                    case ShapePath.FlagsAndCommand.CommandCurve3:
                    case ShapePath.FlagsAndCommand.CommandCurve4:
                        {
                            //transform 2d
                            transformToApply.Transform(ref transformedVertex.position.x, ref transformedVertex.position.y);
                        } break;
                }
                output.Add(transformedVertex);
            }
        }
        public VertexStorage DoTransformToNewVxStorage()
        {
            List<VertexData> data = new List<VertexData>();
            DoTransform(data);
            return new VertexStorage(data);
        }
        public SinglePath DoTransformToNewSinglePath()
        {
            List<VertexData> data = new List<VertexData>();
            DoTransform(data);
            return new SinglePath(new VertexStorage(data), 0);
        }
        public void Rewind(int path_id)
        {
            vtxsrc.Rewind(path_id);
        }
        public void RewindZero()
        {
            vtxsrc.RewindZero();
        }
        public ShapePath.FlagsAndCommand GetNextVertex(out double x, out double y)
        {
            ShapePath.FlagsAndCommand cmd = vtxsrc.GetNextVertex(out x, out y);
            if (ShapePath.IsVertextCommand(cmd))
            {
                transformToApply.Transform(ref x, ref y);
            }
            return cmd;
        }

        //public void SetTransformToApply(Transform.ITransform newTransformToApply)
        //{
        //    transformToApply = newTransformToApply;
        //}
    }
}