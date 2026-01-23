using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hades.Tool
{
    [Serializable]
    public class HandPositionData
    {
        public string spriteName;
        public Vector2 normalizedPosition;
        public Vector2 normalizedSize;
        public Vector2 pixelPosition;
        public Vector2 pixelSize;
        public float rotation = 0f;
        public int sortingOrder = 1;
    }

    [Serializable]
    public class AnimationHandData
    {
        public string animationName;
        public List<HandPositionData> framePositions = new List<HandPositionData>();
    }

    [CreateAssetMenu(fileName = "HandPositionData", menuName = "Hades/Hand Position Data")]
    public class SpriteHandPositionData : ScriptableObject
    {
        public List<AnimationHandData> animationDataList = new List<AnimationHandData>();
        
        [Obsolete("Use animationDataList instead")]
        public string sourceTextureName;
        [Obsolete("Use animationDataList instead")]
        public List<HandPositionData> handPositions = new List<HandPositionData>();
    }
}
