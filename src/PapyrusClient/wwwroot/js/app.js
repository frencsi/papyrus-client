window.clientManagerStorage = {
    get: (key) => {
        const value = window.localStorage.getItem(key);
        return value ? JSON.parse(value) : null;
    },
    set: (key, value) => {
        window.localStorage.setItem(key, JSON.stringify(value));
    }
};

window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}

Blazor.start({
    configureRuntime: runtime => runtime.withConfig({
        loadAllSatelliteResources: true
    })
})