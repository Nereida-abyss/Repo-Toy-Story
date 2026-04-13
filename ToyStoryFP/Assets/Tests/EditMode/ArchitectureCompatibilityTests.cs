using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

public class ArchitectureCompatibilityTests
{
    [Test]
    public void SceneChangeController_RetainsLegacyCompatibility()
    {
        Type legacySceneType = typeof(SceneChangeController).Assembly.GetType("CambioEscena");
        Type legacyVfxType = typeof(DestroyAfterDelay).Assembly.GetType("EliminarVFXScript");

        Assert.That(legacySceneType, Is.Not.Null);
        Assert.That(legacyVfxType, Is.Not.Null);
        Assert.That(legacySceneType.IsSubclassOf(typeof(SceneChangeController)), Is.True);
        Assert.That(legacyVfxType.IsSubclassOf(typeof(DestroyAfterDelay)), Is.True);
    }

    [Test]
    public void PanelController_And_EnemyController_KeepTheirPublicEntryPoints()
    {
        Assert.That(typeof(PanelController).GetMethod("OpenCreditsFromButton", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(EnemyController).GetMethod("ConfigureRuntimeContext", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(EnemyController).GetMethod("ApplyRoundScaling", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(EnemyController).GetMethod("NotifyDamagedByPlayer", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(EnemyController).GetMethod("NotifyAllyAlert", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(EnemyController).GetMethod("SetAvoidancePriority", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
    }

    [Test]
    public void AudioEntryPoints_ExposeNewFeedbackSounds()
    {
        Assert.That(typeof(PlayerAudioController).GetMethod("PlayHitmarker", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(PlayerAudioController).GetMethod("PlayDeath", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(AudioManager).GetMethod("PlayGameplayMusic", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(AudioManager).GetMethod("PlayShopMusic", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(AudioManager).GetMethod("RestoreGameplayMusic", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(AudioManager).GetMethod("GetUiShopPurchaseSuccessClip", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(AudioManager).GetMethod("GetUiShopPurchaseSuccessVolume", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(AudioManager).GetMethod("GetUiShopPurchaseFailedClip", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(AudioManager).GetMethod("GetUiShopPurchaseFailedVolume", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
    }

    [Test]
    public void RoundDialogueManager_UsesCatalogAsSingleRuntimeSource()
    {
        string source = ReadSource("Assets/Project/Scripts/Features/Dialogue/RoundDialogueManager.cs");
        Assert.That(source, Does.Not.Contain("customDialogues"));
        Assert.That(source, Does.Not.Contain("useRandomSentences"));
        Assert.That(source, Does.Not.Contain("repeatSentences"));
        Assert.That(source, Does.Not.Contain("GetRandomSentence"));
    }

    [Test]
    public void SettingsPanelController_DoesNotDeclareLegacyDefaultConstants()
    {
        string source = ReadSource("Assets/Project/Scripts/Features/UI/SettingsPanelController.cs");
        Assert.That(source, Does.Not.Contain("LegacyDefaultVolume"));
        Assert.That(source, Does.Not.Contain("LegacyDefaultLookSensitivity"));
        Assert.That(source, Does.Not.Contain("LegacyDefaultWindowedWidth"));
        Assert.That(source, Does.Not.Contain("LegacyMinimumWindowedDimension"));
    }

    [Test]
    public void PlayerShopController_DoesNotDeclareLegacyBalanceConstants()
    {
        string source = ReadSource("Assets/Project/Scripts/Features/Player/PlayerShopController.cs");
        Assert.That(source, Does.Not.Contain("LegacySharedUpgradePrice"));
        Assert.That(source, Does.Not.Contain("LegacyAmmoPurchaseMagazineCount"));
        Assert.That(source, Does.Not.Contain("LegacyUpgradeStepMultiplier"));
        Assert.That(source, Does.Not.Contain("LegacyHealFraction"));
    }

    [Test]
    public void AudioManager_NoLongerFallsBackToLegacyMusicArrays()
    {
        string source = ReadSource("Assets/Project/Scripts/Features/Audio/AudioManager.cs");
        Assert.That(source, Does.Not.Contain("GetLegacyMusicClip("));
        Assert.That(source, Does.Not.Contain("Se usaran los arrays legacy si existen"));
    }

    [Test]
    public void PlayerController_ResolvesDependenciesBeforeWarning()
    {
        string source = ReadSource("Assets/Project/Scripts/Features/Player/PlayerController.cs");
        Assert.That(source, Does.Contain("ResolveDependencies();"));
        Assert.That(source, Does.Contain("GetComponent<MovementScript>()"));
        Assert.That(source, Does.Contain("GetComponent<PlayerHealthScript>()"));
        Assert.That(source, Does.Contain("GetComponent<PlayerCurrencyController>()"));
        Assert.That(source, Does.Contain("GetComponent<PlayerAudioController>()"));
        Assert.That(source, Does.Contain("GetComponentInChildren<MouseLookScript>(true)"));
        Assert.That(source, Does.Contain("GetComponentInChildren<WeaponLoadoutScript>(true)"));
        Assert.That(source, Does.Contain("GetComponent<PlayerShopController>()"));
        Assert.That(source, Does.Not.Contain("necesita referencias serializadas"));
    }

    [Test]
    public void WaveManager_KeepsStateUpdatesSplitIntoDedicatedMethods()
    {
        string source = ReadSource("Assets/Project/Scripts/Features/Waves/WaveManager.cs");
        Assert.That(source, Does.Contain("InitializeWaveManager()"));
        Assert.That(source, Does.Contain("UpdateWaveProgress(bool isPaused)"));
        Assert.That(source, Does.Contain("UpdateIntermissionProgress(bool isPaused)"));
        Assert.That(source, Does.Contain("ShouldStartNextWaveFromInput()"));
        Assert.That(source, Does.Contain("TickIntermissionTimer()"));
        Assert.That(source, Does.Contain("PrepareNextWaveState()"));
        Assert.That(source, Does.Contain("AdvanceDialogueRound()"));
    }

    [Test]
    public void UiFx_ResolveAudioThroughDedicatedHelpers()
    {
        string buttonSource = ReadSource("Assets/Project/Scripts/Features/UI/UIButtonFx.cs");
        Assert.That(buttonSource, Does.Contain("PlayConfiguredSound(bool isHoverSound)"));
        Assert.That(buttonSource, Does.Contain("ResolveProfileHoverClip()"));
        Assert.That(buttonSource, Does.Contain("ResolveManagerHoverClip(AudioManager resolvedAudioManager)"));

        string panelSource = ReadSource("Assets/Project/Scripts/Features/UI/UIPanelFx.cs");
        Assert.That(panelSource, Does.Contain("PlayConfiguredPanelSound(bool isOpenSound)"));
        Assert.That(panelSource, Does.Contain("ResolveProfileOpenClip()"));
        Assert.That(panelSource, Does.Contain("ResolveManagerOpenClip(AudioManager resolvedAudioManager)"));
        Assert.That(panelSource, Does.Contain("ResolveManagerCloseClip(AudioManager resolvedAudioManager)"));
    }

    [Test]
    public void FinalCleanup_KeepsCompatibilityExplicit_AndHelpersReadable()
    {
        string mouseLookSource = ReadSource("Assets/Project/Scripts/Features/Player/PlayerMouseLookScript.cs");
        Assert.That(mouseLookSource, Does.Contain("public static MouseLookScript Instance => instance;"));

        string waveAnnouncementSource = ReadSource("Assets/Project/Scripts/Features/Waves/WaveAnnouncementUI.cs");
        Assert.That(waveAnnouncementSource, Does.Contain("HasRequiredUiReferences()"));
        Assert.That(waveAnnouncementSource, Does.Contain("ResolveAudioCatalog(AudioManager resolvedAudioManager)"));
        Assert.That(waveAnnouncementSource, Does.Contain("ResolveAnnouncementAudioSource(AudioManager resolvedAudioManager)"));

        string dialogueControllerSource = ReadSource("Assets/Project/Scripts/Features/Dialogue/RoundDialogueController.cs");
        Assert.That(dialogueControllerSource, Does.Contain("HasRequiredUiReferences()"));
        Assert.That(dialogueControllerSource, Does.Contain("SetDialoguePanelVisible(bool isVisible)"));
        Assert.That(dialogueControllerSource, Does.Contain("PauseDialogueFlow()"));
        Assert.That(dialogueControllerSource, Does.Contain("ResumeDialogueFlow()"));
    }

    [Test]
    public void JuniorFriendlyEditing_KeepsHudDataDriven()
    {
        string hudSource = ReadSource("Assets/Project/Scripts/Features/Player/PlayerHUDController.cs");
        Assert.That(hudSource, Does.Contain("[SerializeField] private PlayerHudProfile hudProfile;"));
        Assert.That(hudSource, Does.Contain("ApplyHudProfile()"));
    }

    [Test]
    public void GameplayInputGate_BecomesTheSharedSourceOfGameplayUiBlocking()
    {
        string gateSource = ReadSource("Assets/Project/Scripts/Core/GameplayInputGate.cs");
        Assert.That(gateSource, Does.Contain("public static bool IsBlocked"));
        Assert.That(gateSource, Does.Contain("public static void Acquire(string owner)"));
        Assert.That(gateSource, Does.Contain("public static void Release(string owner)"));
        Assert.That(gateSource, Does.Contain("public static void ApplyGameplayCursorState()"));

        string playerControllerSource = ReadSource("Assets/Project/Scripts/Features/Player/PlayerController.cs");
        Assert.That(playerControllerSource, Does.Contain("GameplayInputGate.IsBlocked"));
        Assert.That(playerControllerSource, Does.Not.Contain("UIManager.IsGamePaused || PlayerShopController.IsInputBlocked"));

        string mouseLookSource = ReadSource("Assets/Project/Scripts/Features/Player/PlayerMouseLookScript.cs");
        Assert.That(mouseLookSource, Does.Contain("GameplayInputGate.IsBlocked"));
        Assert.That(mouseLookSource, Does.Contain("GameplayInputGate.BlockStateChanged += HandlePauseStateChanged;"));

        string uiManagerSource = ReadSource("Assets/Project/Scripts/Features/UI/UIManager.cs");
        Assert.That(uiManagerSource, Does.Contain("GameplayInputGate.Acquire(PauseInputGateOwner);"));
        Assert.That(uiManagerSource, Does.Contain("GameplayInputGate.Release(PauseInputGateOwner);"));

        string shopSource = ReadSource("Assets/Project/Scripts/Features/Player/PlayerShopController.cs");
        Assert.That(shopSource, Does.Contain("public static bool IsInputBlocked => GameplayInputGate.IsBlocked;"));
        Assert.That(shopSource, Does.Contain("GameplayInputGate.Acquire(ShopInputGateOwner);"));
        Assert.That(shopSource, Does.Contain("GameplayInputGate.Release(ShopInputGateOwner);"));

        string settingsSource = ReadSource("Assets/Project/Scripts/Features/UI/SettingsPanelController.cs");
        Assert.That(settingsSource, Does.Contain("GameplayInputGate.Acquire(SettingsInputGateOwner);"));
        Assert.That(settingsSource, Does.Contain("GameplayInputGate.Release(SettingsInputGateOwner);"));

        string dialogueSource = ReadSource("Assets/Project/Scripts/Features/Dialogue/RoundDialogueController.cs");
        Assert.That(dialogueSource, Does.Contain("GameplayInputGate.Acquire(DialogueInputGateOwner);"));
        Assert.That(dialogueSource, Does.Contain("GameplayInputGate.Release(DialogueInputGateOwner);"));
        Assert.That(dialogueSource, Does.Not.Contain("canShoot"));
    }

    [Test]
    public void DialogueFlow_UsesCatalogTiming_And_ClickToCloseLastSentence()
    {
        string catalogSource = ReadSource("Assets/Project/Scripts/Features/Dialogue/WaveDialogueCatalog.cs");
        Assert.That(catalogSource, Does.Contain("sentencePauseDuration"));
        Assert.That(catalogSource, Does.Contain("public float SentencePauseDuration"));

        string managerSource = ReadSource("Assets/Project/Scripts/Features/Dialogue/RoundDialogueManager.cs");
        Assert.That(managerSource, Does.Contain("public float GetSentencePauseDuration()"));
        Assert.That(managerSource, Does.Contain("return dialogueCatalog.SentencePauseDuration;"));

        string controllerSource = ReadSource("Assets/Project/Scripts/Features/Dialogue/RoundDialogueController.cs");
        Assert.That(controllerSource, Does.Contain("GetSentencePauseDuration()"));
        Assert.That(controllerSource, Does.Contain("WaitForDialogueAdvanceInput()"));
        Assert.That(controllerSource, Does.Contain("ProjectInput.WasDialogueAdvancePressed()"));
        Assert.That(controllerSource, Does.Contain("ProjectInput.ConsumePrimaryFireUntilRelease();"));

        string inputSource = ReadSource("Assets/Project/Scripts/Core/ProjectInput.cs");
        Assert.That(inputSource, Does.Contain("public static bool WasDialogueAdvancePressed()"));
        Assert.That(inputSource, Does.Contain("public static void ConsumePrimaryFireUntilRelease()"));
        Assert.That(inputSource, Does.Contain("if (ignorePrimaryFireUntilRelease)"));
    }

    private static string ReadSource(string relativePath)
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        Assert.That(File.Exists(fullPath), Is.True, $"El archivo '{relativePath}' debe existir.");
        return File.ReadAllText(fullPath);
    }
}
