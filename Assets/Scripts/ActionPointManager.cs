using UnityEngine;

namespace FireSale
{
    public class ActionPointManager : MonoBehaviour
    {
        public int maxAP;
        public int currentAP;

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
