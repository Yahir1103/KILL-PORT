using System.Globalization;
using KillPort.App.Settings;

namespace KillPort.App.Localization;

public sealed class LocalizationService
{
    private const string EnglishLanguageCode = "en";
    private const string SpanishLanguageCode = "es";

    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo SpanishCulture = CultureInfo.GetCultureInfo("es-MX");

    private static readonly IReadOnlyDictionary<string, string> EnglishStrings =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AppName"] = "Kill Port",
            ["MenuRefresh"] = "Refresh",
            ["MenuOpenPorts"] = "Open ports",
            ["MenuViewAll"] = "View all...",
            ["MenuFavorites"] = "Favorite processes...",
            ["MenuLanguage"] = "Language",
            ["MenuExit"] = "Exit",
            ["MenuEnglish"] = "English",
            ["MenuSpanish"] = "Espa\u00F1ol",
            ["TrayNone"] = "(none)",
            ["TrayPortItemLabel"] = "{0}{1}  \u2022  {2}  (PID {3})",
            ["NotifyErrorTitle"] = "Kill Port - Error",
            ["DialogConfirmCloseTitle"] = "Confirm close",
            ["DialogConfirmCloseMessage"] =
                "Close the process listening on port {0}?\n\nProcess: {1}\nPID: {2}",
            ["DialogPrivilegesTitle"] = "Insufficient privileges",
            ["DialogPrivilegesMessage"] =
                "Restart Kill Port as administrator to close this process.",
            ["DialogCloseBlockedTitle"] = "Kill Port",
            ["DialogCloseBlockedMessage"] = "This process cannot be closed: {0}",
            ["TitleSuccess"] = "Success",
            ["TitleError"] = "Error",
            ["PortListTitle"] = "Kill Port - Listening ports",
            ["ColumnPinned"] = "\u2605",
            ["ColumnPort"] = "Port",
            ["ColumnAddress"] = "Address",
            ["ColumnProcess"] = "Process",
            ["ColumnPid"] = "PID",
            ["ColumnStatus"] = "Status",
            ["ButtonRefresh"] = "Refresh",
            ["ButtonCloseSelected"] = "Close selected",
            ["StatusOk"] = "OK",
            ["ProcessWindowsTag"] = "Windows",
            ["FavoritesTitle"] = "Kill Port - Favorite processes",
            ["FavoritesPlaceholder"] = "node, python, java...",
            ["FavoritesAdd"] = "Add",
            ["FavoritesRemove"] = "Remove",
            ["FavoritesSaveClose"] = "Save and close",
            ["FavoritesHint"] = "These processes appear first in the list and tray menu.",
            ["ProcessUnresolvable"] = "Process could not be resolved",
            ["ProcessNoExecutable"] = "No executable path",
            ["ProcessWindowsSvchost"] = "Windows process (svchost)",
            ["ProcessSystem"] = "System process",
            ["ClosePortSuccess"] = "Port {0} was closed successfully.",
            ["ClosePortAccessDenied"] = "Access denied while closing {0} (PID {1}).",
            ["ClosePortAlreadyExited"] = "The process had already exited.",
            ["ClosePortUnexpectedError"] = "Unexpected error: {0}",
        };

    private static readonly IReadOnlyDictionary<string, string> SpanishStrings =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AppName"] = "Kill Port",
            ["MenuRefresh"] = "Actualizar",
            ["MenuOpenPorts"] = "Puertos abiertos",
            ["MenuViewAll"] = "Ver todos...",
            ["MenuFavorites"] = "Procesos favoritos...",
            ["MenuLanguage"] = "Idioma",
            ["MenuExit"] = "Salir",
            ["MenuEnglish"] = "English",
            ["MenuSpanish"] = "Espa\u00F1ol",
            ["TrayNone"] = "(ninguno)",
            ["TrayPortItemLabel"] = "{0}{1}  \u2022  {2}  (PID {3})",
            ["NotifyErrorTitle"] = "Kill Port - Error",
            ["DialogConfirmCloseTitle"] = "Confirmar cierre",
            ["DialogConfirmCloseMessage"] =
                "\u00BFCerrar el proceso que escucha en el puerto {0}?\n\nProceso: {1}\nPID: {2}",
            ["DialogPrivilegesTitle"] = "Privilegios insuficientes",
            ["DialogPrivilegesMessage"] =
                "Reinicia Kill Port como administrador para cerrar este proceso.",
            ["DialogCloseBlockedTitle"] = "Kill Port",
            ["DialogCloseBlockedMessage"] = "No se puede cerrar: {0}",
            ["TitleSuccess"] = "\u00C9xito",
            ["TitleError"] = "Error",
            ["PortListTitle"] = "Kill Port - Puertos en escucha",
            ["ColumnPinned"] = "\u2605",
            ["ColumnPort"] = "Puerto",
            ["ColumnAddress"] = "Direcci\u00F3n",
            ["ColumnProcess"] = "Proceso",
            ["ColumnPid"] = "PID",
            ["ColumnStatus"] = "Estado",
            ["ButtonRefresh"] = "Actualizar",
            ["ButtonCloseSelected"] = "Cerrar seleccionado",
            ["StatusOk"] = "OK",
            ["ProcessWindowsTag"] = "Windows",
            ["FavoritesTitle"] = "Kill Port - Procesos favoritos",
            ["FavoritesPlaceholder"] = "node, python, java...",
            ["FavoritesAdd"] = "Agregar",
            ["FavoritesRemove"] = "Eliminar",
            ["FavoritesSaveClose"] = "Guardar y cerrar",
            ["FavoritesHint"] =
                "Estos procesos aparecen primero en la lista y en el men\u00FA.",
            ["ProcessUnresolvable"] = "Proceso no resoluble",
            ["ProcessNoExecutable"] = "Sin ruta ejecutable",
            ["ProcessWindowsSvchost"] = "Proceso de Windows (svchost)",
            ["ProcessSystem"] = "Proceso del sistema",
            ["ClosePortSuccess"] = "Puerto {0} cerrado correctamente.",
            ["ClosePortAccessDenied"] = "Acceso denegado al cerrar {0} (PID {1}).",
            ["ClosePortAlreadyExited"] = "El proceso ya hab\u00EDa terminado.",
            ["ClosePortUnexpectedError"] = "Error inesperado: {0}",
        };

    private readonly SettingsService _settings;
    private string _languageCode;

    public LocalizationService(SettingsService settings)
    {
        _settings = settings;
        _languageCode = NormalizeLanguageCode(settings.Current.Language);
    }

    public event EventHandler? LanguageChanged;

    public string CurrentLanguageCode => _languageCode;

    public bool IsLanguage(string languageCode) =>
        string.Equals(_languageCode, NormalizeLanguageCode(languageCode), StringComparison.OrdinalIgnoreCase);

    public string Get(string key)
    {
        var dictionary = GetDictionary(_languageCode);
        if (dictionary.TryGetValue(key, out var value))
            return value;

        if (EnglishStrings.TryGetValue(key, out value))
            return value;

        return key;
    }

    public string Format(string key, params object[] args) =>
        string.Format(GetCulture(_languageCode), Get(key), args);

    public void SetLanguage(string languageCode)
    {
        string normalized = NormalizeLanguageCode(languageCode);
        if (string.Equals(_languageCode, normalized, StringComparison.OrdinalIgnoreCase))
            return;

        _languageCode = normalized;
        _settings.Current.Language = normalized;
        _settings.Save();

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static IReadOnlyDictionary<string, string> GetDictionary(string languageCode) =>
        string.Equals(languageCode, SpanishLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? SpanishStrings
            : EnglishStrings;

    private static CultureInfo GetCulture(string languageCode) =>
        string.Equals(languageCode, SpanishLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? SpanishCulture
            : EnglishCulture;

    private static string NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return GetDefaultLanguageCode();

        string normalized = languageCode.Trim().ToLowerInvariant();

        if (normalized.StartsWith(SpanishLanguageCode, StringComparison.Ordinal))
            return SpanishLanguageCode;

        if (normalized.StartsWith(EnglishLanguageCode, StringComparison.Ordinal))
            return EnglishLanguageCode;

        return GetDefaultLanguageCode();
    }

    private static string GetDefaultLanguageCode() =>
        string.Equals(
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
            SpanishLanguageCode,
            StringComparison.OrdinalIgnoreCase)
            ? SpanishLanguageCode
            : EnglishLanguageCode;
}
