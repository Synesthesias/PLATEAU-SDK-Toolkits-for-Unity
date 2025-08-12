using UnityEngine;

namespace PlateauToolkit.Sandbox.Runtime
{
    public class PlateauSandboxAdvertisementScaled : PlateauSandboxPlaceableHandler
    {
        // 対象の広告物オブジェクトはbillboard部とpole部それぞれスケール1倍で1mとする

        [SerializeField]
        private GameObject billboardRoot;

        [SerializeField]
        private GameObject poleRoot;

        private Vector3 billboardDefaultPosition;
        private Vector3 poleDefaultPosition;

        public Vector3 BillboardSize
        {
            get => billboardRoot.transform.localScale;
            set
            {
                billboardRoot.transform.localScale = value;
            }
        }

        public float PoleHeight
        {
            get => poleRoot.transform.localScale.y;
            set
            {
                poleRoot.transform.localScale = new Vector3(poleRoot.transform.localScale.x, value, poleRoot.transform.localScale.z);

                // 高さの値に応じてbillboardRootのy座標を変更する
                float y = poleRoot.transform.localPosition.y + poleRoot.transform.localScale.y;
                billboardRoot.transform.localPosition = new Vector3(billboardRoot.transform.localPosition.x, y, billboardRoot.transform.localPosition.z);
            }
        }

        private void Awake()
        {
            billboardDefaultPosition = billboardRoot.transform.position;
            poleDefaultPosition = poleRoot.transform.position;
        }
    }
}