using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.Graphing
{
    public class GroupData : Group
    {
        [SerializeField]
        Guid m_Guid;

        public Guid guid
        {
            get { return m_Guid; }
        }

        [NonSerialized]
        Group m_Group;

        public Group group
        {
            get { return m_Group; }
        }

        public GroupData(Group group)
        {
            m_Guid = Guid.NewGuid();
            m_Group = group;
        }
    }
}

