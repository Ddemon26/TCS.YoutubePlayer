[UxmlElement] public partial class DependencyContainer : VisualElement {
    #region Class Names
    public static readonly string ClassNameUSS = "dependency-container";

    public static readonly string RootUSS = ClassNameUSS + "_root"; // dependency-container_root 
    public static readonly string HeaderContainerUSS = ClassNameUSS + "_header-container"; // dependency-container_header-container 
    public static readonly string HeaderUSS = ClassNameUSS + "_header"; // dependency-container_header 
    public static readonly string InfoUSS = ClassNameUSS + "_info"; // dependency-container_info 
    public static readonly string InformationFoldoutUSS = ClassNameUSS + "_information-foldout"; // dependency-container_information-foldout 
    public static readonly string InformationLabelUSS = ClassNameUSS + "_information-label"; // dependency-container_information-label 
    public static readonly string InstalledContainerUSS = ClassNameUSS + "_installed-container"; // dependency-container_installed-container 
    public static readonly string InstalledLabelUSS = ClassNameUSS + "_installed-label"; // dependency-container_installed-label 
    public static readonly string SpacingUSS = ClassNameUSS + "_spacing"; // dependency-container_spacing 
    public static readonly string InstallTextureUSS = ClassNameUSS + "_install-texture"; // dependency-container_install-texture 
    public static readonly string VersionContainerUSS = ClassNameUSS + "_version-container"; // dependency-container_version-container 
    public static readonly string VersionLabelUSS = ClassNameUSS + "_version-label"; // dependency-container_version-label 
    public static readonly string Spacing2USS = ClassNameUSS + "_spacing-2"; // dependency-container_spacing-2 
    public static readonly string VersionValueLabelUSS = ClassNameUSS + "_version-value-label"; // dependency-container_version-value-label 
    public static readonly string InsallContainerUSS = ClassNameUSS + "_insall-container"; // dependency-container_insall-container 
    public static readonly string InstallButtonUSS = ClassNameUSS + "_install-button"; // dependency-container_install-button 
    public static readonly string UpdateButtonUSS = ClassNameUSS + "_update-button"; // dependency-container_update-button 
    public static readonly string UninstallButtonUSS = ClassNameUSS + "_uninstall-button"; // dependency-container_uninstall-button 
    #endregion

    #region UI Elements
    readonly VisualElement m_root = new() { name = "Root" };
    readonly VisualElement m_headerContainer = new() { name = "HeaderContainer" };
    readonly Label m_header = new() { name = "Header" };
    readonly VisualElement m_info = new() { name = "Info" };
    readonly Foldout m_informationFoldout = new() { name = "InformationFoldout" };
    readonly Label m_informationLabel = new() { name = "InformationLabel" };
    readonly VisualElement m_installedContainer = new() { name = "InstalledContainer" };
    readonly Label m_installedLabel = new() { name = "InstalledLabel" };
    readonly VisualElement m_spacing = new() { name = "Spacing" };
    readonly VisualElement m_installTexture = new() { name = "InstallTexture" };
    readonly VisualElement m_versionContainer = new() { name = "VersionContainer" };
    readonly Label m_versionLabel = new() { name = "VersionLabel" };
    readonly VisualElement m_spacing2 = new() { name = "Spacing" };
    readonly Label m_versionValueLabel = new() { name = "VersionValueLabel" };
    readonly VisualElement m_insallContainer = new() { name = "InsallContainer" };
    readonly Button m_installButton = new() { name = "InstallButton" };
    readonly Button m_updateButton = new() { name = "UpdateButton" };
    readonly Button m_uninstallButton = new() { name = "UninstallButton" };
    #endregion

    #region Actions
    public Action OnInstallButtonClicked;
    public Action OnUpdateButtonClicked;
    public Action OnUninstallButtonClicked;
    #endregion

    #region Constructor
    public DependencyContainer() {
        SetElementClassNames();

        // Set Text Fields
        m_header.text = "Header";
        m_informationFoldout.text = "Information";
        m_informationLabel.text = "Ytl-dip is used for converting url into unity playable videos.";
        m_installedLabel.text = "Installed:";
        m_versionLabel.text = "Current Version:";
        m_versionValueLabel.text = "Unknown";
        m_installButton.text = "Install";
        m_updateButton.text = "Update";
        m_uninstallButton.text = "Uninstall";

        // Build Hierarchy
        hierarchy.Add( m_root );
        m_root.Add( m_headerContainer );
        m_headerContainer.Add( m_header );
        m_root.Add( m_info );
        m_info.Add( m_informationFoldout );
        m_informationFoldout.Add( m_informationLabel );
        m_root.Add( m_installedContainer );
        m_installedContainer.Add( m_installedLabel );
        m_installedContainer.Add( m_spacing );
        m_installedContainer.Add( m_installTexture );
        m_root.Add( m_versionContainer );
        m_versionContainer.Add( m_versionLabel );
        m_versionContainer.Add( m_spacing2 );
        m_versionContainer.Add( m_versionValueLabel );
        m_root.Add( m_insallContainer );
        m_insallContainer.Add( m_installButton );
        m_insallContainer.Add( m_updateButton );
        m_insallContainer.Add( m_uninstallButton );

        m_informationFoldout.value = false;
        SetInstallTextureResult( false );
    }

    void SetElementClassNames() {
        AddToClassList( ClassNameUSS );
        m_root.AddToClassList( RootUSS );
        m_headerContainer.AddToClassList( HeaderContainerUSS );
        m_header.AddToClassList( HeaderUSS );
        m_info.AddToClassList( InfoUSS );
        m_informationFoldout.AddToClassList( InformationFoldoutUSS );
        m_informationLabel.AddToClassList( InformationLabelUSS );
        m_installedContainer.AddToClassList( InstalledContainerUSS );
        m_installedLabel.AddToClassList( InstalledLabelUSS );
        m_spacing.AddToClassList( SpacingUSS );
        m_installTexture.AddToClassList( InstallTextureUSS );
        m_versionContainer.AddToClassList( VersionContainerUSS );
        m_versionLabel.AddToClassList( VersionLabelUSS );
        m_spacing2.AddToClassList( Spacing2USS );
        m_versionValueLabel.AddToClassList( VersionValueLabelUSS );
        m_insallContainer.AddToClassList( InsallContainerUSS );
        m_installButton.AddToClassList( InstallButtonUSS );
        m_updateButton.AddToClassList( UpdateButtonUSS );
        m_uninstallButton.AddToClassList( UninstallButtonUSS );
    }
    #endregion

    #region Callbacks
    public void RegisterCallbacks() {
        m_installButton.clicked += InstallPressed;
        m_updateButton.clicked += UpdatePressed;
        m_uninstallButton.clicked += UninstalledPressed;
    }
    void UninstalledPressed() => OnUninstallButtonClicked?.Invoke();
    void UpdatePressed() => OnUpdateButtonClicked?.Invoke();
    void InstallPressed() => OnInstallButtonClicked?.Invoke();

    public void UnregisterCallbacks() {
        m_installButton.clicked -= InstallPressed;
        m_updateButton.clicked -= UpdatePressed;
        m_uninstallButton.clicked -= UninstalledPressed;
    }
    #endregion

    #region Public API
    public void SetInstallTextureResult(bool isInstalled)
        => m_installTexture.style.unityBackgroundImageTintColor = isInstalled ? new StyleColor( Color.green ) : new StyleColor( Color.red );

    public void SetHeaderText(string header) => m_header.text = header;
    public void SetInformationText(string info) => m_informationLabel.text = info;
    public void SetVersionValue(string version) => m_versionValueLabel.text = version;
    public void ToggleCurrentVersionVisibility(bool isVisible)
        => m_versionContainer.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
    public void ToggleEnabled(bool isEnabled) {
        m_installButton.SetEnabled( isEnabled );
        m_updateButton.SetEnabled( isEnabled );
        m_uninstallButton.SetEnabled( isEnabled );
    }
    #endregion
}