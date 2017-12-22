using UnityEngine;
using System.Collections;

namespace CBG {
    public class RewindPreviewerUIHelper : MonoBehaviour {
        RewindPreviewer[] previewers;
        [SerializeField]
        float maxPreviewDelay = 5;

        // Use this for initialization
        void Start() {
            previewers = GameObject.FindObjectsOfType<RewindPreviewer>();
        }

        public void UpdatePreviewers(float normalizedDelay) {
            for (int i=0;i<previewers.Length;i++) {
                previewers[i].timeDelay = normalizedDelay * maxPreviewDelay;
            }
        }
    }
}