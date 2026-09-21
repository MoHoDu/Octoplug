using UnityEngine;

namespace Octoplug.Balance
{
    public static class BalanceRegistry
    {
        private static GameBalanceArchive instance;

        public static GameBalanceArchive Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameBalanceArchive>("Balance/GameBalanceArchive");
                    if (instance == null)
                    {
                        Debug.LogError("[Balance] GameBalanceArchive not found in Resources/Balance/. Did you sync from Google Sheets?");
                    }
                }
                return instance;
            }
        }
    }
}
