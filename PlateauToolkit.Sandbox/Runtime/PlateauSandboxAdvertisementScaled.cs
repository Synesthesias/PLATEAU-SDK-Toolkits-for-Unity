using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

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


        // 画像と動画表示用
        public int targetMaterialNumber;
        public string targetTextureProperty = "_MainTex";
        public PlateauSandboxAdvertisement.AdvertisementType advertisementType;
        public List<PlateauSandboxAdvertisement.AdvertisementMaterials> advertisementMaterials;
        public Texture advertisementTexture;
        public VideoClip advertisementVideoClip;
        public VideoPlayer VideoPlayer { get; private set; }

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

        public void AddVideoPlayer()
        {
            if (!gameObject.TryGetComponent(out VideoPlayer videoPlayer))
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
                videoPlayer.isLooping = true;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.clip = advertisementVideoClip;
                VideoPlayer = videoPlayer;
            }
            else
            {
                VideoPlayer = videoPlayer;
            }
        }

        public void SetTexture()
        {
            if (advertisementMaterials.Count <= 0)
            {
                return;
            }

            Material mat = advertisementMaterials[0].materials[targetMaterialNumber];
            if (mat.HasProperty(targetTextureProperty))
            {
                // RenderTextureは設定しない
                Texture texture = mat.GetTexture(targetTextureProperty);
                if (texture as RenderTexture == null)
                {
                    advertisementTexture = texture;
                }
            }
        }

        public void SetMaterials()
        {
            MeshRenderer[] lsChildMeshRender = transform.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer childMeshRender in lsChildMeshRender)
            {
                IEnumerable<PlateauSandboxAdvertisement.AdvertisementMaterials> matchingMaterials =
                    advertisementMaterials.Where(m => m.gameObjectName == childMeshRender.transform.name);

                foreach (PlateauSandboxAdvertisement.AdvertisementMaterials advertisementMaterial in matchingMaterials)
                {
                    childMeshRender.sharedMaterials = advertisementMaterial.materials.ToArray();
                }
            }
        }

        public void Reset()
        {
            advertisementMaterials = new List<PlateauSandboxAdvertisement.AdvertisementMaterials>();
            MeshRenderer[] lsChildMeshRender = transform.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer childMeshRender in lsChildMeshRender)
            {
                var advertisementMaterial = new PlateauSandboxAdvertisement.AdvertisementMaterials
                {
                    gameObjectName = childMeshRender.transform.name,
                    materials = new List<Material>(childMeshRender.sharedMaterials)
                };
                advertisementMaterials.Add(advertisementMaterial);
            }

            SetTexture();
            AddVideoPlayer();
        }

        // private void Awake()
        // {
        //     //billboardDefaultPosition = billboardRoot.transform.position;
        //     //poleDefaultPosition = poleRoot.transform.position;
        // }

    }
}