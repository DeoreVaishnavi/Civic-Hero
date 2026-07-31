export const notify=(message,severity="success")=>window.dispatchEvent(new CustomEvent("civichero:toast",{detail:{message,severity}}));
