/// <reference path="~/GeneratedArtifacts/viewModel.js" />

myapp.ViewProjectFiles.DownloadFile_execute = function (screen) {
    // Write code here.
    var root = location.protocol + '//' + location.host;
    var contentItem = screen.findContentItem("Id");
    var fileId = contentItem.value.toString();

    var appname = location.pathname.substr(0, window.location.pathname.lastIndexOf('/HTMLClient/default.htm'));

    window.location.href = root + appname + '/DownloadFile.ashx?fileId=' + fileId;

};
myapp.ViewProjectFiles.DeleteFile_execute = function (screen) {
    // Write code here.
    if(confirm("ნამდვილად გინდათ ფაილის წაშლა?")) {
        screen.ProjectFiles.deleteEntity();
        return myapp.activeDataWorkspace.ApplicationData.saveChanges().then(function success() {
            // If success.
            window.history.back();
        }, function fail(e) {
            throw e;
        });
    }

};