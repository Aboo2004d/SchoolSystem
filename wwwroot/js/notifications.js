const notyf = new Notyf({
    duration: 4000,
    position: { x: 'center', y: 'top' },
    dismissible: true
});

function notifySuccess(message) {
    notyf.success(message);
}

function notifyError(message) {
    notyf.error(message);
}

function notifyWarning(message) {
    notyf.warning(message);
}