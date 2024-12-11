using System;
using System.Collections.Generic;

namespace PlateauToolkit.Sandbox.Runtime.ElectricPost
{
    public struct PlateauSandboxElectricConnectInfo
    {
        public PlateauSandboxElectricPost m_Target;
        public bool m_IsFront;
        public int m_Index;
    }

    /// <summary>
    /// 電柱の情報
    /// </summary>
    public class PlateauSandboxElectricPostInfo
    {
        private List<PlateauSandboxElectricConnectInfo> m_FrontConnectedPosts = new();
        public List<PlateauSandboxElectricConnectInfo> FrontConnectedPosts => m_FrontConnectedPosts;

        private List<PlateauSandboxElectricConnectInfo> m_BackConnectedPosts = new();
        public List<PlateauSandboxElectricConnectInfo> BackConnectedPosts => m_BackConnectedPosts;

        public int AddConnectionSpace(bool isFront)
        {
            if (isFront)
            {
                m_FrontConnectedPosts.Add(new PlateauSandboxElectricConnectInfo());
                return m_FrontConnectedPosts.Count - 1;
            }
            else
            {
                m_BackConnectedPosts.Add(new PlateauSandboxElectricConnectInfo());
                return m_BackConnectedPosts.Count - 1;
            }
        }

        public void ResetConnection(bool isFront, int index)
        {
            if (isFront)
            {
                if (index >= 0 && index < m_FrontConnectedPosts.Count)
                {
                    m_FrontConnectedPosts[index] = new PlateauSandboxElectricConnectInfo();
                }
            }
            else
            {
                if (index >= 0 && index < m_BackConnectedPosts.Count)
                {
                    m_BackConnectedPosts[index] = new PlateauSandboxElectricConnectInfo();
                }
            }
        }

        public void RemoveConnection(bool isFront, int index)
        {
            if (isFront)
            {
                if (index >= 0 && index < m_FrontConnectedPosts.Count)
                {
                    m_FrontConnectedPosts.RemoveAt(index);
                }
            }
            else
            {
                if (index >= 0 && index < m_BackConnectedPosts.Count)
                {
                    m_BackConnectedPosts.RemoveAt(index);
                }
            }
        }

        public void SetFrontConnect(PlateauSandboxElectricPost other, bool isOtherFront, int index, int otherIndex)
        {
            if (m_FrontConnectedPosts.Count > index)
            {
                m_FrontConnectedPosts[index] = new PlateauSandboxElectricConnectInfo()
                {
                    m_Target = other, m_IsFront = isOtherFront, m_Index = otherIndex
                };
            }
        }

        public void SetBackConnect(PlateauSandboxElectricPost other, bool isOtherFront, int index, int otherIndex)
        {
            if (m_BackConnectedPosts.Count > index)
            {
                m_BackConnectedPosts[index] = new PlateauSandboxElectricConnectInfo()
                {
                    m_Target = other, m_IsFront = isOtherFront, m_Index = otherIndex
                };
            }
        }

        public PlateauSandboxElectricConnectInfo GetConnectedPost(bool isFront, int index)
        {
            if (isFront)
            {
                if (index >= 0 && index < m_FrontConnectedPosts.Count)
                {
                    return m_FrontConnectedPosts[index];
                }
            }
            else
            {
                if (index >= 0 && index < m_BackConnectedPosts.Count)
                {
                    return m_BackConnectedPosts[index];
                }
            }
            return new PlateauSandboxElectricConnectInfo();
        }

        public string GetNextWireID()
        {
            return Guid.NewGuid().ToString();
        }
    }
}