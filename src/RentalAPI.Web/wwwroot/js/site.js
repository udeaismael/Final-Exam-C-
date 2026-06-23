// RentaKey - helpers globales
document.addEventListener('DOMContentLoaded', function () {
    // Cierre automático de alerts
    document.querySelectorAll('.alert').forEach(function (el) {
        setTimeout(function () {
            var alert = bootstrap.Alert.getOrCreateInstance(el);
            alert.close();
        }, 5000);
    });
});
