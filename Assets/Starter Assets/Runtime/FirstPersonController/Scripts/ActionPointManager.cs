using System;
using UnityEngine;


namespace StarterAssets
{
    public class ActionPointManager : MonoBehaviour
    {
        public int maxAP;
        public int currentAP;
        public static ActionPointManager instance;
        public event Action<int> OnAPUpdated;

        private void Awake()
        {
            instance = this;
        }

        public void UseAP()
        {
            currentAP--;
            if (currentAP < 0) currentAP = 0;
            OnAPUpdated?.Invoke(currentAP);
        }

        public void RegenAP()
        {
            currentAP++;
            if (currentAP > maxAP) currentAP = maxAP;
            OnAPUpdated?.Invoke(currentAP);

        }

        public int GetAP() { return currentAP; }
    }
}
