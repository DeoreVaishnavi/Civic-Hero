export function detectLanguage(){const saved=localStorage.getItem("civichero-language");return saved||navigator.language?.split("-")[0]||"en"}
