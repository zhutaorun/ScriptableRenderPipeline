using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class GroupData : ISerializationCallbackReceiver
    {
        [NonSerialized]
        Guid m_Guid;

        public Guid guid
        {
            get { return m_Guid; }
        }

        [SerializeField]
        string m_GuidSerialized;

        [SerializeField]
        string m_Title;

        public string title
        {
            get{ return m_Title; }
        }

        public GroupData(string title)
        {
            m_Guid = Guid.NewGuid();
            m_Title = title;
        }
//        [NonSerialized]
//        Group m_Group;
//
//        public Group group
//        {
//            get { return m_Group; }
//        }
//
//        public GroupData(Group group)
//        {
//            m_Guid = Guid.NewGuid();
//            m_Group = group;
//        }

        public void OnBeforeSerialize()
        {
            m_GuidSerialized = guid.ToString();
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_GuidSerialized))
            {
                m_Guid = new Guid(m_GuidSerialized);
            }
        }
    }
}

