using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendance
{
    public sealed class ShellConfirmationDialog0843 : MonoBehaviour
    {
        private GameObject root;
        private Text titleText;
        private Text bodyText;
        private Text statusText;
        private Button primaryButton;
        private Button secondaryButton;
        private Button cancelButton;
        private Action primaryAction;
        private Action secondaryAction;
        private Action cancelAction;

        public bool IsOpen => root != null && root.activeSelf;
        public string DebugTitle0843 => titleText?.text ?? string.Empty;
        public string DebugBody0843 => bodyText?.text ?? string.Empty;
        public string DebugStatus0843 => statusText?.text ?? string.Empty;
        public bool DebugPrimaryInteractable0843 => primaryButton != null && primaryButton.interactable;

        public static ShellConfirmationDialog0843 Create(Transform parent, string name="Confirmation0843")
        {
            var host=new GameObject(name,typeof(RectTransform));host.transform.SetParent(parent,false);
            var dialog=host.AddComponent<ShellConfirmationDialog0843>();dialog.Build();return dialog;
        }

        private void Build()
        {
            var rt=GetComponent<RectTransform>();rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
            root=ShellUIFactory.PanelRect(transform,"ConfirmationBackdrop",Vector2.zero,Vector2.one,new Color(0f,0f,0f,0.74f));
            var frame=ShellUIFactory.PanelRect(root.transform,"ConfirmationFrame",new Vector2(0.28f,0.28f),new Vector2(0.72f,0.72f),new Color(0.018f,0.025f,0.034f,0.995f));
            titleText=ShellUIFactory.TextRect(frame.transform,"CONFIRM",28,TextAnchor.MiddleCenter,new Vector2(0.06f,0.76f),new Vector2(0.94f,0.93f),ShellUIFactory.Gold);
            bodyText=ShellUIFactory.TextRect(frame.transform,"",17,TextAnchor.MiddleCenter,new Vector2(0.08f,0.42f),new Vector2(0.92f,0.75f),ShellUIFactory.Ivory);
            bodyText.resizeTextForBestFit=true;bodyText.resizeTextMinSize=10;bodyText.resizeTextMaxSize=17;
            statusText=ShellUIFactory.TextRect(frame.transform,"",13,TextAnchor.MiddleCenter,new Vector2(0.08f,0.33f),new Vector2(0.92f,0.42f),new Color(0.70f,0.74f,0.78f));
            primaryButton=ShellUIFactory.ButtonRect(frame.transform,"CONFIRM",new Vector2(0.07f,0.13f),new Vector2(0.36f,0.29f),InvokePrimary);
            secondaryButton=ShellUIFactory.ButtonRect(frame.transform,"SECONDARY",new Vector2(0.37f,0.13f),new Vector2(0.66f,0.29f),InvokeSecondary);
            cancelButton=ShellUIFactory.ButtonRect(frame.transform,"CANCEL",new Vector2(0.67f,0.13f),new Vector2(0.93f,0.29f),InvokeCancel);
            root.SetActive(false);
        }

        public void Show(string title,string body,string primaryLabel,Action onPrimary,string secondaryLabel,Action onSecondary,string status="",bool primaryEnabled=true,string cancelLabel="CANCEL",Action onCancel=null)
        {
            primaryAction=onPrimary;secondaryAction=onSecondary;cancelAction=onCancel;
            titleText.text=title??"CONFIRM";bodyText.text=body??string.Empty;statusText.text=status??string.Empty;
            primaryButton.GetComponentInChildren<Text>().text=primaryLabel??"CONFIRM";primaryButton.interactable=primaryEnabled;primaryButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(primaryLabel));
            secondaryButton.GetComponentInChildren<Text>().text=secondaryLabel??string.Empty;secondaryButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(secondaryLabel));
            cancelButton.GetComponentInChildren<Text>().text=string.IsNullOrWhiteSpace(cancelLabel)?"CANCEL":cancelLabel;
            root.SetActive(true);root.transform.SetAsLastSibling();
        }

        public void SetStatus(string message,bool error=false)
        {
            if(statusText==null)return;statusText.text=message??string.Empty;statusText.color=error?new Color(0.95f,0.48f,0.38f):new Color(0.72f,0.78f,0.70f);
        }

        public void Hide(){if(root!=null)root.SetActive(false);primaryAction=null;secondaryAction=null;cancelAction=null;}
        public void DebugCancel0843()=>InvokeCancel();

        private void InvokePrimary(){primaryAction?.Invoke();}
        private void InvokeSecondary(){secondaryAction?.Invoke();}
        private void InvokeCancel(){var a=cancelAction;Hide();a?.Invoke();}
    }
}
