using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    /// <summary> NPC 대화 전체 데이터 </summary>
    [Serializable]
    public class DialogData
    {
        public int npcId;
        public string npcName;
        public string speakerIconkey;
        public List<DialogNode> nodes;
    }

    /// <summary> 하나의 대화 노드 (한 화면에 표시될 내용) </summary>
    [Serializable]
    public class DialogNode
    {
        public int nodeId;
        public string dialogText;
        public List<DialogChoice> choices;
        public bool allowPrev = false;
    }

    /// <summary> 대화 선택지 </summary>
    [Serializable]
    public class DialogChoice
    {
        public string choiceText;
        public int nextNodeId;
        public string choiceId = "";
    }
}
