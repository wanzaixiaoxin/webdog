use std::path::PathBuf;

/// Locates a `runtime/` folder shipped next to the executable and, if present,
/// points WebView2 at it via the `WEBVIEW2_BROWSER_EXECUTABLE_FOLDER` env var.
///
/// This lets the portable distribution run on machines without the WebView2
/// runtime installed: the user just keeps `runtime/` next to `webdog.exe`.
/// Must run *before* Tauri initializes the webview.
fn setup_portable_webview2() {
    // Only override if the user hasn't set it themselves.
    if std::env::var_os("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER").is_some() {
        return;
    }

    let exe_dir = match std::env::current_exe().ok().and_then(|p| p.parent().map(PathBuf::from)) {
        Some(dir) => dir,
        None => return,
    };

    let runtime_dir = exe_dir.join("runtime");
    if runtime_dir.is_dir() {
        std::env::set_var("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER", &runtime_dir);
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    setup_portable_webview2();

    tauri::Builder::default()
        .plugin(tauri_plugin_http::init())
        .plugin(tauri_plugin_websocket::init())
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
