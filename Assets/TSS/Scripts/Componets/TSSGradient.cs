// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TSS
{
    [AddComponentMenu("TSS/Gradient")]
    public class TSSGradient : BaseMeshEffect
    {
        #region Properties

        public enum Direction { horizontal, vertical, invertHorizontal, invertVertical }

        [SerializeField, Range(0, 1)] private float _offset = 0;
        public float Offset
        {
            get { return _offset; }
            set
            {
                if (autoInvertDirection)
                {
                    if (value == 1)
                    {
                        if (direction == Direction.horizontal) direction = Direction.invertHorizontal;
                        if (direction == Direction.vertical) direction = Direction.invertVertical;
                    }
                    if (value == 0)
                    {
                        if (direction == Direction.invertHorizontal) direction = Direction.horizontal;
                        if (direction == Direction.invertVertical) direction = Direction.vertical;
                    }
                }

                _offset = value;
                graphic.SetVerticesDirty();
            }
        }

        [SerializeField]
        private Gradient _effectGradient = new Gradient()
        {
            colorKeys = new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.white, 1) }
        };

        public Gradient EffectGradient
        {
            get { return _effectGradient; }
            set
            {
                _effectGradient = value;
                graphic.SetVerticesDirty();
            }
        }

        public Direction direction;
        public bool autoInvertDirection;

        #endregion

        #region Vertex coloring

        Color BlendColor(Color colorA, Color colorB)
        {
            return colorA * colorB;
        }

        private void EvaluateHorizontal(VertexHelper helper, float offset, bool invert = false)
        {
            List<UIVertex> _vertexList = new List<UIVertex>();

            helper.GetUIVertexStream(_vertexList);

            int nCount = _vertexList.Count;

            float left = _vertexList[0].position.x;
            float right = _vertexList[0].position.x;
            float x = 0f;

            for (int i = nCount - 1; i >= 1; --i)
            {
                x = _vertexList[i].position.x;
                if (x > right) right = x;
                else if (x < left) left = x;
            }

            float width = 1f / (right - left);
            UIVertex vertex = new UIVertex();

            for (int i = 0; i < helper.currentVertCount; i++)
            {
                helper.PopulateUIVertex(ref vertex, i);
                if (invert)
                {
                    vertex.color = EffectGradient.Evaluate(1 - ((vertex.position.x - left) * width - (2 - offset * 2 - 1)));
                }
                else
                {
                    vertex.color = EffectGradient.Evaluate((vertex.position.x - left) * width - (offset * 2 - 1));
                }
                helper.SetUIVertex(vertex, i);
            }
        }

        private void EvaluateVertical(VertexHelper helper, float offset, bool invert = false)
        {
            List<UIVertex> _vertexList = new List<UIVertex>();

            helper.GetUIVertexStream(_vertexList);

            int nCount = _vertexList.Count;

            float bottom = _vertexList[0].position.y;
            float top = _vertexList[0].position.y;
            float y = 0f;

            for (int i = nCount - 1; i >= 1; --i)
            {
                y = _vertexList[i].position.y;

                if (y > top) top = y;
                else if (y < bottom) bottom = y;
            }

            float height = 1f / (top - bottom);
            UIVertex vertex = new UIVertex();

            for (int i = 0; i < helper.currentVertCount; i++)
            {
                helper.PopulateUIVertex(ref vertex, i);

                if (invert)
                {
                    vertex.color = EffectGradient.Evaluate(1 - ((vertex.position.y - bottom) * height - (2 - offset * 2 - 1)));
                }
                else
                {
                    vertex.color = EffectGradient.Evaluate((vertex.position.y - bottom) * height - (offset * 2 - 1));
                }
                helper.SetUIVertex(vertex, i);
            }

        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || helper.currentVertCount == 0) return;

            switch (direction)
            {
                case Direction.horizontal: EvaluateHorizontal(helper, Offset); break;
                case Direction.vertical: EvaluateVertical(helper, Offset); break;
                case Direction.invertHorizontal: EvaluateHorizontal(helper, Offset, true); break;
                case Direction.invertVertical: EvaluateVertical(helper, Offset, true); break;
            }
        }

        #endregion
    }
}