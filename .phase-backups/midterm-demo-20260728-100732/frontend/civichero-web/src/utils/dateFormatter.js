export function formatDate(value,locale="en-IN"){return new Intl.DateTimeFormat(locale,{dateStyle:"medium",timeStyle:"short"}).format(new Date(value))}
