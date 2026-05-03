export function extractHubError(error) {
    return error.message.split("HubException: ").pop();
}