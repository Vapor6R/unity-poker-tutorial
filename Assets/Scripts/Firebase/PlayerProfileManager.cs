using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Photon.Pun;
using Photon.Realtime;

public class PlayerProfileManager : MonoBehaviourPun
{
    public static PlayerProfileManager Instance { get; private set; }

    [Serializable]
    public class PlayerProfile
    {
        public string uid;
        public string displayName;
        public string referralCode;
        public string gender;           // "male" or "female"
        public string country;          // e.g. "DZ"
        public int    level;
        public long   xp;
        public long   balance;
        public int    handsPlayed;
        public long   biggestPot;
        public float  winPercent;       // 0-100
        public long   jackpotWon;
        public int    bjpWon;
        public string bestHand;         // e.g. "Royal Flush"
        public string bestHandCards;    // e.g. "AS KS QS JS TS"

        public override string ToString() =>
            $"[Profile] {displayName} | {country} | Level {level} | Balance {balance}";
    }

    public PlayerProfile Profile { get; private set; }
    public bool          IsReady  { get; private set; }

    private static readonly string[] Adjectives =
    {
        "Swift", "Bold", "Silent", "Lucky", "Clever", "Wild", "Brave",
        "Shadow", "Mighty", "Golden", "Iron", "Storm", "Frost", "Blaze",
        "Dark", "Neon", "Crimson", "Silver", "Phantom", "Royal"
    };

    private static readonly string[] Nouns =
    {
        "Shark", "Wolf", "Eagle", "Tiger", "Fox", "Cobra", "Bear",
        "Hawk", "Viper", "Lion", "Panda", "Raven", "Falcon", "Drake",
        "Ace", "King", "Ghost", "Rider", "Bluff", "Dealer"
    };

private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(Instance.gameObject); // destroy OLD
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);
}
public void UpdateDisplayName(string newName, System.Action<bool, string> callback)
{
    if (string.IsNullOrEmpty(newName))
    {
        callback?.Invoke(false, "Name is empty.");
        return;
    }
 
    // Adjust the path to match your database schema, e.g. "players/{uid}/displayName"
    string uid  = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
    if (string.IsNullOrEmpty(uid))
    {
        callback?.Invoke(false, "Not authenticated.");
        return;
    }
 
    string path = $"players/{uid}/displayName";
 
    FirebaseDatabase.DefaultInstance
        .GetReference(path)
        .SetValueAsync(newName)
        .ContinueWith(task =>
        {
            // Firebase callbacks are NOT on the Unity main thread — dispatch safely
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    string err = task.Exception?.Message ?? "Unknown error";
                    Debug.LogError($"[ProfileManager] UpdateDisplayName failed: {err}");
                    callback?.Invoke(false, err);
                }
                else
                {
                    // Update the local profile so the rest of the app stays in sync
                    if (Profile != null) Profile.displayName = newName;
                    Debug.Log($"[ProfileManager] Display name updated to: {newName}");
                    callback?.Invoke(true, "");
                }
            });
        });
}
    private void Start()
    {
        StartCoroutine(InitProfile());
    }

    private IEnumerator InitProfile()
    {
        yield return new WaitUntil(() =>
            FirebaseAuth.DefaultInstance.CurrentUser != null &&
            FirebaseAuth.DefaultInstance.CurrentUser.IsValid());

        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        string uid = user.UserId;

        DatabaseReference dbRef = FirebaseDatabase.DefaultInstance
            .GetReference("players")
            .Child(uid);

        var readTask = dbRef.GetValueAsync();
        yield return new WaitUntil(() => readTask.IsCompleted);

        if (readTask.IsFaulted)
        {
            Debug.LogError($"[Profile] Failed to read: {readTask.Exception}");
            yield break;
        }

        DataSnapshot snap = readTask.Result;

        if (snap.Exists)
        {
            Profile = new PlayerProfile
            {
                uid           = uid,
                displayName   = snap.Child("displayName").Value?.ToString()   ?? "Player",
                referralCode  = snap.Child("referralCode").Value?.ToString()  ?? "ABC123",
                gender        = snap.Child("gender").Value?.ToString()        ?? "male",
                country       = snap.Child("country").Value?.ToString()       ?? DetectCountry(),
                level         = ParseInt(snap.Child("level").Value),
                xp            = ParseLong(snap.Child("xp").Value),
                balance       = ParseLong(snap.Child("balance").Value),
                handsPlayed   = ParseInt(snap.Child("handsPlayed").Value),
                biggestPot    = ParseLong(snap.Child("biggestPot").Value),
                winPercent    = ParseFloat(snap.Child("winPercent").Value),
                jackpotWon    = ParseLong(snap.Child("jackpotWon").Value),
                bjpWon        = ParseInt(snap.Child("bjpWon").Value),
                bestHand      = snap.Child("bestHand").Value?.ToString()      ?? "—",
                bestHandCards = snap.Child("bestHandCards").Value?.ToString() ?? ""
            };
            Debug.Log($"[Profile] Loaded — {Profile}");
        }
        else
        {
            Profile = new PlayerProfile
            {
                uid           = uid,
                displayName   = GenerateRandomName(),
                referralCode  = GenerateReferralCode(),
                gender        = "male",
                country       = DetectCountry(),
                level         = 0,
                xp            = 0,
                balance       = 1000,
                handsPlayed   = 0,
                biggestPot    = 0,
                winPercent    = 0f,
                jackpotWon    = 0,
                bjpWon        = 0,
                bestHand      = "—",
                bestHandCards = ""
            };

            var writeTask = dbRef.SetValueAsync(ProfileToDict(Profile));
            yield return new WaitUntil(() => writeTask.IsCompleted);

            if (writeTask.IsFaulted)
                Debug.LogError($"[Profile] Write failed: {writeTask.Exception}");
            else
                Debug.Log($"[Profile] New profile created — {Profile}");
        }

        IsReady = true;
    }

    // ── Persist helpers ──────────────────────────────────────────

    private System.Collections.Generic.Dictionary<string, object> ProfileToDict(PlayerProfile p)
    {
        return new System.Collections.Generic.Dictionary<string, object>
        {
            { "uid",           p.uid           },
            { "displayName",   p.displayName   },
            { "referralCode",  p.referralCode  },
            { "gender",        p.gender        },
            { "country",       p.country       },
            { "level",         p.level         },
            { "xp",            p.xp            },
            { "balance",       p.balance       },
            { "handsPlayed",   p.handsPlayed   },
            { "biggestPot",    p.biggestPot    },
            { "winPercent",    p.winPercent    },
            { "jackpotWon",    p.jackpotWon    },
            { "bjpWon",        p.bjpWon        },
            { "bestHand",      p.bestHand      },
            { "bestHandCards", p.bestHandCards }
        };
    }

    public void SaveProfile()
    {
        if (Profile == null) return;
        FirebaseDatabase.DefaultInstance
            .GetReference("players")
            .Child(Profile.uid)
            .UpdateChildrenAsync(ProfileToDict(Profile));
    }

    // ── Public stat updaters (call from game systems) ─────────────

    public void AddXP(long amount)
    {
        if (Profile == null) return;
        Profile.xp    += amount;
        Profile.level  = (int)(Profile.xp / 1000);
        PatchDB(new() { { "xp", Profile.xp }, { "level", Profile.level } });
    }

    public void SetGender(string gender)
    {
        if (Profile == null) return;
        Profile.gender = gender;
        PatchDB(new() { { "gender", gender } });
    }

    public void RecordHandResult(bool won, long potSize)
    {
        if (Profile == null) return;
        Profile.handsPlayed++;
        if (won)
        {
            int wins = Mathf.RoundToInt(Profile.winPercent / 100f * (Profile.handsPlayed - 1));
            wins++;
            Profile.winPercent = (float)wins / Profile.handsPlayed * 100f;
            if (potSize > Profile.biggestPot) Profile.biggestPot = potSize;
        }
        else
        {
            int wins = Mathf.RoundToInt(Profile.winPercent / 100f * (Profile.handsPlayed - 1));
            Profile.winPercent = (float)wins / Profile.handsPlayed * 100f;
        }
        PatchDB(new() {
            { "handsPlayed", Profile.handsPlayed },
            { "winPercent",  Profile.winPercent  },
            { "biggestPot",  Profile.biggestPot  }
        });
    }

    public void RecordBestHand(string handName, string cards)
    {
        if (Profile == null) return;
        Profile.bestHand      = handName;
        Profile.bestHandCards = cards;
        PatchDB(new() { { "bestHand", handName }, { "bestHandCards", cards } });
    }

    public void AddJackpot(long amount)
    {
        if (Profile == null) return;
        Profile.jackpotWon += amount;
        PatchDB(new() { { "jackpotWon", Profile.jackpotWon } });
    }

    public void AddBJP()
    {
        if (Profile == null) return;
        Profile.bjpWon++;
        PatchDB(new() { { "bjpWon", Profile.bjpWon } });
    }

    public void UpdateBalance(long newBalance)
    {
        if (Profile == null) return;
        Profile.balance = newBalance;
        PatchDB(new() { { "balance", Profile.balance } });
    }

    // ── Generators ───────────────────────────────────────────────

    private static string GenerateRandomName()
    {
        System.Random rng = new System.Random();
        return Adjectives[rng.Next(Adjectives.Length)] +
               Nouns[rng.Next(Nouns.Length)] +
               rng.Next(10, 9999);
    }

    private static string GenerateReferralCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        System.Random rng = new System.Random();
        char[] code = new char[6];
        for (int i = 0; i < 6; i++)
            code[i] = chars[rng.Next(chars.Length)];
        return new string(code);
    }

    private static string DetectCountry()
    {
        try
        {
            RegionInfo region = new RegionInfo(CultureInfo.CurrentCulture.Name);
            return region.TwoLetterISORegionName;  // e.g. "DZ", "FR", "US"
        }
        catch
        {
            return "XX";
        }
    }

    // ── DB patch helper ──────────────────────────────────────────

    private void PatchDB(System.Collections.Generic.Dictionary<string, object> fields)
    {
        if (Profile == null) return;
        FirebaseDatabase.DefaultInstance
            .GetReference("players")
            .Child(Profile.uid)
            .UpdateChildrenAsync(fields);
    }

    // ── Parse helpers ────────────────────────────────────────────

    private static int   ParseInt(object v)   => v == null ? 0    : int.Parse(v.ToString());
    private static long  ParseLong(object v)  => v == null ? 0L   : long.Parse(v.ToString());
    private static float ParseFloat(object v) => v == null ? 0f   : float.Parse(v.ToString());
}