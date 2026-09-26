using UnityEngine;


namespace StarterAssets
{
    public class ActionPointManager : MonoBehaviour
    {
        public int maxAP;
        public int currentAP;
        public static ActionPointManager instance;

        private void Awake()
        {
            instance = this;
        }

        public void UseAP()
        {
            currentAP--;
            if (currentAP < 0) currentAP = 0;
        }

        public void RegenAP()
        {
            currentAP++;
            if (currentAP > maxAP) currentAP = maxAP;
        }

        public int GetAP() { return currentAP; }
    }
}
