/// <reference path="~/GeneratedArtifacts/viewModel.js" />

myapp.BrowseDebtReportDatas.Refresh_execute = function (screen) {
    // Write code here.
    myapp.activeDataWorkspace.ApplicationData.DebtReportCommands.addNew();
    myapp.activeDataWorkspace.ApplicationData.saveChanges().then(
        function success() {
            // If success.
            screen.DebtReportDatas.refresh();
        }, function fail(e) {
            throw e;
        });

};
myapp.BrowseDebtReportDatas.Excel_execute = function (screen) {
    // Write code here.
    var dataworkspace = new msls.application.DataWorkspace;
    var xls = dataworkspace.ApplicationData.ExcelExports.addNew();
    xls.TableName = "DebtReport";

    dataworkspace.ApplicationData.saveChanges().then(
        function () {
            var documentBlob = xls.ExcelData;
            var blob = base64toBlob(documentBlob);
            var reportName = 'DebtReport-' + new Date().toLocaleString() + '.xlsx';
            invokeSaveAsDialog(blob, reportName);
        }
    )

};