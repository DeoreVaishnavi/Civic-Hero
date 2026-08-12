import {translations} from "./i18n.js"; export async function loadTranslations(language="en"){return translations[language]||translations.en}
