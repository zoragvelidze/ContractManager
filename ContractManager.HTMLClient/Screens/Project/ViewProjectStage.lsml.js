/// <reference path="~/GeneratedArtifacts/viewModel.js" />

myapp.ViewProjectStage.DeleteStage_execute = function (screen) {
    // Write code here.
    if (confirm("ნამდვილად გინდათ პროექტის ეტაპის წაშლა?")) {
        screen.ProjectStage.deleteEntity();
        return myapp.activeDataWorkspace.ApplicationData.saveChanges().then(function success() {
            // If success.
            window.history.back();
        }, function fail(e) {
            throw e;
        });
    }

};
myapp.ViewProjectStage.DeleteSalary_execute = function (screen) {
    // Write code here.
    screen.Salaries.deleteSelected();
    return myapp.activeDataWorkspace.ApplicationData.saveChanges().then(function success() {
        // If success.
    }, function fail(e) {
        throw e;
    });

};
myapp.ViewProjectStage.DeleteSalary_canExecute = function (screen) {
    // Write code here.
    return screen.Salaries.selectedItem !== null;
};
myapp.ViewProjectStage.DeleteMaterial_execute = function (screen) {
    // Write code here.
    screen.Materials.deleteSelected();
    return myapp.activeDataWorkspace.ApplicationData.saveChanges().then(function success() {
        // If success.
    }, function fail(e) {
        throw e;
    });

};
myapp.ViewProjectStage.DeleteMaterial_canExecute = function (screen) {
    // Write code here.
    return screen.Materials.selectedItem !== null;
};