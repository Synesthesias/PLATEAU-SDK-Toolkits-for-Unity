using PlateauToolkit.Editor;
using PlateauToolkit.Sandbox.Runtime;
using PlateauToolkit.Sandbox.Runtime.ElectricPost;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace PlateauToolkit.Sandbox.Editor
{
    [CustomEditor(typeof(PlateauSandboxElectricPost))]
    public class PlateauSandboxElectricPostInspector : UnityEditor.Editor
    {
        private PlateauSandboxElectricPostContext m_Context;
        private PlateauSandboxElectricPost m_Target;

        private PlateauSandboxElectricPostConnectionGUI m_FrontConnectionGUI;
        private PlateauSandboxElectricPostConnectionGUI m_BackConnectionGUI;
        private PlateauSandboxElectricPostKeyEvent m_KeyEvent;

        private void OnEnable()
        {
            m_Target = target as PlateauSandboxElectricPost;
            m_Context = PlateauSandboxElectricPostContext.GetCurrent();
            m_Context.OnSelected.AddListener(ResetSelect);
            m_KeyEvent = new PlateauSandboxElectricPostKeyEvent();

            SetGUI();
        }

        private void SetGUI()
        {
            if (m_FrontConnectionGUI == null)
            {
                m_FrontConnectionGUI = new PlateauSandboxElectricPostConnectionGUI(m_Target, true, m_KeyEvent);
                m_FrontConnectionGUI.OnClickSelect.AddListener(SelectingPost);
                // m_FrontConnectionGUI.OnClickRelease.AddListener(() => TryReleaseWire(true));
                // m_FrontConnectionGUI.OnDirectSelect.AddListener((post) => m_Target.SetFrontConnectPointToFacing(post));
            }
            if (m_BackConnectionGUI == null)
            {
                m_BackConnectionGUI = new PlateauSandboxElectricPostConnectionGUI(m_Target, false, m_KeyEvent);
                m_BackConnectionGUI.OnClickSelect.AddListener(SelectingPost);
                // m_BackConnectionGUI.OnClickRelease.AddListener(() => TryReleaseWire(false));
                // m_BackConnectionGUI.OnDirectSelect.AddListener((post) => m_Target.SetBackConnectPointToFacing(post));
            }
        }

        public override void OnInspectorGUI()
        {
            if (m_Context == null || m_Target == null)
            {
                return;
            }

            serializedObject.Update();
            base.OnInspectorGUI();

            m_FrontConnectionGUI.DrawLayout(m_Target.FrontConnectedPosts);
            m_BackConnectionGUI.DrawLayout(m_Target.BackConnectedPosts);

            // キーイベント
            if (m_KeyEvent.IsEscapeKey())
            {
                ResetSelect();
            }

            if (m_KeyEvent.IsFocusDelete(out PlateauSandboxElectricPost post))
            {
                m_Target.RemoveConnectedPost(post);
                post.RemoveConnectedPost(m_Target);
                ResetSelect();
            }
        }

        private void SelectingPost(bool isSelecting)
        {
            if (isSelecting)
            {
                // 選択中状態に
                SetActiveTool();
            }
            else
            {
                ResetSelect();
            }
        }

        private void ResetSelect()
        {
            GUIUtility.hotControl = 0;
            Event.current.Use();

            ToolManager.RestorePreviousPersistentTool();
            m_FrontConnectionGUI.Reset();
            m_BackConnectionGUI.Reset();

            m_Context.OnCancel.Invoke();
        }

        private void SetActiveTool()
        {
            if (ToolManager.activeToolType != typeof(PlateauSandboxElectricPostSelectTool))
            {
                ToolManager.SetActiveTool<PlateauSandboxElectricPostSelectTool>();
            }
            else
            {
                ToolManager.RestorePreviousPersistentTool();
            }
        }
    }
}