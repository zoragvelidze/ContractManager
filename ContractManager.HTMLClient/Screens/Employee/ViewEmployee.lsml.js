/// <reference path="~/GeneratedArtifacts/viewModel.js" />

myapp.ViewEmployee.Details_postRender = function (element, contentItem) {
    // Write code here.
    var name = contentItem.screen.Employee.details.getModel()[':@SummaryProperty'].property.name;
    contentItem.dataBind("screen.Employee." + name, function (value) {
        contentItem.screen.details.displayName = value;
    });
}

