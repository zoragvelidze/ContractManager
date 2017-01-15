/// <reference path="~/GeneratedArtifacts/viewModel.js" />

myapp.ViewClient.Details_postRender = function (element, contentItem) {
    // Write code here.
    var name = contentItem.screen.Client.details.getModel()[':@SummaryProperty'].property.name;
    contentItem.dataBind("screen.Client." + name, function (value) {
        contentItem.screen.details.displayName = value;
    });
}

