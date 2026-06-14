using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Google;

/// <summary>
/// FirebaseAuthManager
/// Attach to a persistent GameObject in your Login/Splash scene.
///
/// Supports:
///   • Google Sign-In  (via Google Sign-In Unity Plugin)
///   • Anonymous Sign-In
///   • Auto-login if user is already signed in
///   • Scene load on successful authentication
///
/// Required packages:
///   • Firebase Auth Unity SDK  (FirebaseAuth.unitypackage)
///   • Google Sign-In Unity Plugin  (https://github.com/googlesamples/google-signin-unity)
///   • google-services.json / GoogleService-Info.plist in Assets/
/// </summary>
public class FirebaseAuthManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  Inspector fields
    // ─────────────────────────────────────────────────────────────

    [Header("Firebase / Google Config")]
    [Tooltip("Web Client ID from google-services.json  (oauth_client with client_type == 3)")]
    [SerializeField] private string webClientId = "190326388740-t6uhpa00essvpso07on2mr7cl6qnhs6f.apps.googleusercontent.com";

    [Header("Scene")]
    [Tooltip("Exact name of the scene to load after successful sign-in")]
    [SerializeField] private string sceneToLoad = "MainMenu";

    [Header("UI (optional — assign in Inspector)")]
    [SerializeField] private Button  googleSignInButton;
    [SerializeField] private Button  anonymousSignInButton;
    [SerializeField] private Button  signOutButton;
    [SerializeField] private Text    statusText;          // legacy UI Text
    [SerializeField] private GameObject loadingOverlay;   // spinner panel

    // ─────────────────────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────────────────────

    private FirebaseAuth   _auth;
    private FirebaseUser   _user;
    private bool           _firebaseReady = false;

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        SetLoading(true);
        SetStatus("Initializing Firebase…");

        // Wire up buttons
        if (googleSignInButton)    googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);
        if (anonymousSignInButton) anonymousSignInButton.onClick.AddListener(OnAnonymousSignInClicked);
        if (signOutButton)         signOutButton.onClick.AddListener(OnSignOutClicked);

        SetButtonsInteractable(false);
    }

    private void Start()
    {
        InitializeFirebase();
    }

    private void OnDestroy()
    {
        if (_auth != null)
        {
            _auth.StateChanged -= OnAuthStateChanged;
            _auth = null;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Firebase initialization
    // ─────────────────────────────────────────────────────────────

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            // Marshal back to main thread
            var dependencyStatus = task.Result;

            RunOnMainThread(() =>
            {
                if (dependencyStatus == DependencyStatus.Available)
                {
                    _auth = FirebaseAuth.DefaultInstance;
                    _auth.StateChanged += OnAuthStateChanged;

                    // Configure Google Sign-In
                    GoogleSignIn.Configuration = new GoogleSignInConfiguration
                    {
                        RequestIdToken  = true,
                        RequestEmail    = true,
                        WebClientId     = webClientId
                    };

                    _firebaseReady = true;
                    SetButtonsInteractable(true);
                    SetLoading(false);

                    // If already signed in, go straight to game
                    if (_auth.CurrentUser != null && _auth.CurrentUser.IsValid())
                    {
                        Debug.Log($"[Auth] Already signed in as {_auth.CurrentUser.DisplayName} ({_auth.CurrentUser.UserId})");
                        SetStatus($"Welcome back, {_auth.CurrentUser.DisplayName ?? "Player"}!");
                        LoadGameScene();
                    }
                    else
                    {
                        SetStatus("Please sign in to continue.");
                    }
                }
                else
                {
                    Debug.LogError($"[Auth] Firebase dependencies not available: {dependencyStatus}");
                    SetStatus($"Firebase error: {dependencyStatus}");
                    SetLoading(false);
                }
            });
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  Auth state listener
    // ─────────────────────────────────────────────────────────────

    private void OnAuthStateChanged(object sender, EventArgs e)
    {
        if (_auth.CurrentUser != _user)
        {
            bool wasSignedIn = _user    != null;
            bool isSignedIn  = _auth.CurrentUser != null && _auth.CurrentUser.IsValid();

            if (wasSignedIn && !isSignedIn)
            {
                Debug.Log("[Auth] User signed out.");
                RunOnMainThread(() => SetStatus("Signed out."));
            }

            _user = _auth.CurrentUser;

            if (isSignedIn)
            {
                Debug.Log($"[Auth] Signed in: {_user.DisplayName} | UID: {_user.UserId} | Anonymous: {_user.IsAnonymous}");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Google Sign-In
    // ─────────────────────────────────────────────────────────────

    private void OnGoogleSignInClicked()
    {
        if (!_firebaseReady) return;
        SetButtonsInteractable(false);
        SetLoading(true);
        SetStatus("Opening Google Sign-In…");
        SignInWithGoogle();
    }

    private void SignInWithGoogle()
    {
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleSignInResult);
    }

    private void OnGoogleSignInResult(System.Threading.Tasks.Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted || task.IsCanceled)
        {
            string reason = task.IsCanceled ? "Cancelled" : task.Exception?.InnerException?.Message ?? "Unknown error";
            Debug.LogWarning($"[Auth] Google Sign-In failed: {reason}");
            RunOnMainThread(() =>
            {
                SetStatus($"Google Sign-In failed: {reason}");
                SetLoading(false);
                SetButtonsInteractable(true);
            });
            return;
        }

        // Exchange Google ID token for a Firebase credential
        string idToken = task.Result.IdToken;
        Credential credential = GoogleAuthProvider.GetCredential(idToken, null);
        SignInWithFirebaseCredential(credential, "Google");
    }

    // ─────────────────────────────────────────────────────────────
    //  Anonymous Sign-In
    // ─────────────────────────────────────────────────────────────

    private void OnAnonymousSignInClicked()
    {
        if (!_firebaseReady) return;
        SetButtonsInteractable(false);
        SetLoading(true);
        SetStatus("Signing in anonymously…");
        SignInAnonymously();
    }

    private void SignInAnonymously()
    {
        _auth.SignInAnonymouslyAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string reason = task.Exception?.InnerException?.Message ?? "Unknown error";
                Debug.LogWarning($"[Auth] Anonymous Sign-In failed: {reason}");
                RunOnMainThread(() =>
                {
                    SetStatus($"Anonymous sign-in failed: {reason}");
                    SetLoading(false);
                    SetButtonsInteractable(true);
                });
                return;
            }

            FirebaseUser result = task.Result.User;
            Debug.Log($"[Auth] Anonymous sign-in success. UID: {result.UserId}");
            RunOnMainThread(() =>
            {
                SetStatus("Signed in anonymously. Loading game…");
                LoadGameScene();
            });
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  Firebase credential sign-in (shared by Google + future providers)
    // ─────────────────────────────────────────────────────────────

    private void SignInWithFirebaseCredential(Credential credential, string providerName)
    {
        _auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string reason = task.Exception?.InnerException?.Message ?? "Unknown error";
                Debug.LogWarning($"[Auth] Firebase sign-in with {providerName} failed: {reason}");
                RunOnMainThread(() =>
                {
                    SetStatus($"Sign-in failed: {reason}");
                    SetLoading(false);
                    SetButtonsInteractable(true);
                });
                return;
            }

            RunOnMainThread(() =>
            {
                FirebaseUser firebaseUser = _auth.CurrentUser;
                Debug.Log($"[Auth] {providerName} sign-in success. Name: {firebaseUser?.DisplayName} | UID: {firebaseUser?.UserId}");
                SetStatus($"Welcome, {firebaseUser?.DisplayName ?? "Player"}! Loading game…");
                LoadGameScene();
            });
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  Sign Out
    // ─────────────────────────────────────────────────────────────

    private void OnSignOutClicked()
    {
        if (_auth == null) return;

        // Also sign out of Google to force account picker next time
        GoogleSignIn.DefaultInstance.SignOut();
        _auth.SignOut();

        SetStatus("Signed out.");
        SetButtonsInteractable(true);
        Debug.Log("[Auth] Signed out.");
    }

    // ─────────────────────────────────────────────────────────────
    //  Scene loading
    // ─────────────────────────────────────────────────────────────

    private void LoadGameScene()
    {
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        SetLoading(true);
        SetButtonsInteractable(false);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            yield return null;
        }

        // Small visual pause so the loading overlay is visible
        yield return new WaitForSeconds(0.3f);
        op.allowSceneActivation = true;
    }

    // ─────────────────────────────────────────────────────────────
    //  UI helpers
    // ─────────────────────────────────────────────────────────────

    private void SetStatus(string message)
    {
        if (statusText) statusText.text = message;
        Debug.Log($"[Auth] {message}");
    }

    private void SetLoading(bool show)
    {
        if (loadingOverlay) loadingOverlay.SetActive(show);
    }

    private void SetButtonsInteractable(bool on)
    {
        if (googleSignInButton)    googleSignInButton.interactable    = on;
        if (anonymousSignInButton) anonymousSignInButton.interactable = on;
        if (signOutButton)         signOutButton.interactable         = on;
    }

    // ─────────────────────────────────────────────────────────────
    //  Thread utility — Firebase callbacks run off the main thread
    // ─────────────────────────────────────────────────────────────

    // Simple dispatcher: queues work to run on Update()
    private readonly System.Collections.Generic.Queue<Action> _mainThreadQueue
        = new System.Collections.Generic.Queue<Action>();

    private void Update()
    {
        while (_mainThreadQueue.Count > 0)
        {
            _mainThreadQueue.Dequeue()?.Invoke();
        }
    }

    private void RunOnMainThread(Action action)
    {
        if (action == null) return;
        lock (_mainThreadQueue)
        {
            _mainThreadQueue.Enqueue(action);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Public accessors (use from other scripts if needed)
    // ─────────────────────────────────────────────────────────────

    /// <summary>Returns the currently signed-in Firebase user, or null.</summary>
    public FirebaseUser CurrentUser => _auth?.CurrentUser;

    /// <summary>True if the current user authenticated anonymously.</summary>
    public bool IsAnonymous => _auth?.CurrentUser?.IsAnonymous ?? false;

    /// <summary>
    /// Call this from elsewhere to upgrade an anonymous account to Google.
    /// Preserves the anonymous UID so existing data isn't lost.
    /// </summary>
    public void LinkAnonymousAccountWithGoogle()
    {
        if (_auth?.CurrentUser == null || !_auth.CurrentUser.IsAnonymous)
        {
            Debug.LogWarning("[Auth] LinkAnonymousAccountWithGoogle: no anonymous user to link.");
            return;
        }

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled) return;

            Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
            _auth.CurrentUser.LinkWithCredentialAsync(credential).ContinueWith(linkTask =>
            {
                if (linkTask.IsFaulted)
                {
                    Debug.LogWarning($"[Auth] Link failed: {linkTask.Exception?.InnerException?.Message}");
                    return;
                }
                Debug.Log($"[Auth] Anonymous account linked to Google. UID: {linkTask.Result.User.UserId}");
            });
        });
    }
}