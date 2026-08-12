export function mapError(error){return error?.response?.data?.message||error?.message||"Something went wrong. Please try again."}

/**
 * Converts API, Axios, fetch and JavaScript errors into readable text.
 */
export function mapApiError(error, fallbackMessage = "Something went wrong.") {
  if (!error) {
    return fallbackMessage;
  }

  if (typeof error === "string") {
    return error;
  }

  const responseData = error?.response?.data;

  if (typeof responseData === "string" && responseData.trim()) {
    return responseData;
  }

  if (typeof responseData?.message === "string" && responseData.message.trim()) {
    return responseData.message;
  }

  if (typeof responseData?.detail === "string" && responseData.detail.trim()) {
    return responseData.detail;
  }

  if (typeof responseData?.title === "string" && responseData.title.trim()) {
    return responseData.title;
  }

  if (responseData?.errors && typeof responseData.errors === "object") {
    const validationMessages = Object.values(responseData.errors)
      .flat()
      .filter(Boolean);

    if (validationMessages.length > 0) {
      return validationMessages.join(" ");
    }
  }

  if (typeof error?.message === "string" && error.message.trim()) {
    return error.message;
  }

  return fallbackMessage;
}
