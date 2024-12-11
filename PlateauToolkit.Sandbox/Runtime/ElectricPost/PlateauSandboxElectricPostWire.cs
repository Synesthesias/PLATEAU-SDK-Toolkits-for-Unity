using UnityEngine;

namespace PlateauToolkit.Sandbox.Runtime.ElectricPost
{
    /// <summary>
    /// 電柱のワイヤー
    /// </summary>
    public class PlateauSandboxElectricPostWire
    {
        private PlateauSandboxElectricPostWireType m_WireType;
        public PlateauSandboxElectricPostWireType WireType => m_WireType;

        private bool m_IsFrontWire;
        public bool IsFrontWire => m_IsFrontWire;

        private GameObject m_ElectricWire;
        public GameObject ElectricWire => m_ElectricWire;
        public Vector3 WirePosition => m_ElectricWire.transform.position;

        private Quaternion m_DefaultLocalRotate;
        private float m_WireScaleSize;

        private PlateauSandboxElectricPost m_TargetPost;
        private bool m_TargetIsFront;

        private int m_Index;
        public int Index => m_Index;

        private string m_WireID;
        public string WireID => m_WireID;

        public PlateauSandboxElectricPostWire(GameObject wire, int index = -1)
        {
            m_ElectricWire = wire;
            m_Index = index;
            m_WireType = PlateauSandboxElectricPostWireTypeExtensions.GetWireType(wire);
            m_IsFrontWire = PlateauSandboxElectricPostWireTypeExtensions.IsFrontWire(wire);

            if (m_WireType == PlateauSandboxElectricPostWireType.k_InValid)
            {
                return;
            }

            m_DefaultLocalRotate = m_ElectricWire.transform.localRotation;

            if (wire.TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                m_WireScaleSize = meshRenderer.bounds.size.magnitude;
            }
        }

        public void TryShow(int index)
        {
            if (m_Index == index)
            {
                Show(true);
            }
        }

        public void Show(bool isShow)
        {
            m_ElectricWire.SetActive(isShow);
        }

        public void SetTarget(PlateauSandboxElectricPost post, bool isFront)
        {
            m_TargetPost = post;
            m_TargetIsFront = isFront;
        }

        public bool IsTarget(PlateauSandboxElectricPost post, bool isFront)
        {
            return m_TargetPost == post && m_TargetIsFront == isFront;
        }

        public void SetElectricNode(Vector3 position)
        {
            Show(true);
            RotateWire(position);
            ScaleWire(position);
        }

        private void RotateWire(Vector3 position)
        {
            // Wireの向きをポイントに向ける
            m_ElectricWire.transform.LookAt(position);

            // 回転を加算
            m_ElectricWire.transform.localRotation = m_ElectricWire.transform.localRotation * m_DefaultLocalRotate;
        }

        private void ScaleWire(Vector3 position)
        {
            // 2点間の距離を求める
            float distance = Vector3.Distance(m_ElectricWire.transform.position, position);
            if (distance <= 0 || m_WireScaleSize <= 0)
            {
                return;
            }

            // Wireの長さを変更
            m_ElectricWire.transform.localScale =
                new Vector3(m_ElectricWire.transform.localScale.x, distance / m_WireScaleSize, m_ElectricWire.transform.localScale.z);
        }

        public void TryHide(int index)
        {
            if (m_Index == index)
            {
                m_ElectricWire.transform.localScale = new Vector3(1, 1, 1);
                Show(false);
            }
        }

        public void SetWireID(string wireID)
        {
            m_WireID = wireID;
        }

        public string RemoveWireID()
        {
            string wireID = m_WireID;
            m_WireID = string.Empty;
            return wireID;
        }

        public void Remove()
        {
#if UNITY_EDITOR
            GameObject.DestroyImmediate(m_ElectricWire);
#else
            GameObject.Destroy(m_ElectricWire);
#endif
        }
    }
}