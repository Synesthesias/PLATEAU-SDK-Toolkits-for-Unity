using System;
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

        //private Vector3 billboardDefaultPosition;
        //private Vector3 poleDefaultPosition;

        /// <summary>
        /// ワールド空間でのサイズ
        /// </summary>
        public Vector3 BillboardSize
        {
            get
            {
                return Vector3.Scale(transform.localScale, billboardRoot.transform.localScale);
            }
            set
            {
                Vector3 v = new Vector3(value.x / transform.lossyScale.x, value.y / transform.lossyScale.y, value.z / transform.lossyScale.z);
                billboardRoot.transform.localScale = v;
            }
        }

        /// <summary>
        /// ワールド空間でのサイズ
        /// </summary>
        public float PoleHeight
        {
            get
            {
                return poleRoot.transform.lossyScale.y;
            }
            set
            {
                float y = value / transform.lossyScale.y;
                poleRoot.transform.localScale = new Vector3(poleRoot.transform.localScale.x, y, poleRoot.transform.localScale.z);

                // 高さの値に応じてbillboardRootのy座標を変更する
                billboardRoot.transform.localPosition = new Vector3(billboardRoot.transform.localPosition.x, poleRoot.transform.localPosition.y + y, billboardRoot.transform.localPosition.z);
            }
        }

        public bool TransformChanged
        {
            get
            {
                bool isChanged = poleRoot.transform.hasChanged == true || billboardRoot.transform.hasChanged == true;
                return isChanged;
            }
            //set;
        }

        public void ResetTransformChangedStatus()
        {
            poleRoot.transform.hasChanged = false;
            billboardRoot.transform.hasChanged = false;
        }

        private void Awake()
        {
            //billboardDefaultPosition = billboardRoot.transform.position;
            //poleDefaultPosition = poleRoot.transform.position;
        }

    }
}