using UnityEngine;
using UnityEngine.UI;

namespace Visometry.VisionLib.SDK.UI
{
    [RequireComponent(typeof(Toggle))]
    [AddComponentMenu("VisionLib/UI/Extend Toggle Events")]
    public class ExtendToggleEvents : MonoBehaviour
    {
        public UnityEngine.Events.UnityEvent onValueChangedToFalse;

        void Start()
        {
            GetComponent<Toggle>().onValueChanged.AddListener(value => {
                if (!value)
                {
                    onValueChangedToFalse.Invoke();
                }
            });
        }
    }
}