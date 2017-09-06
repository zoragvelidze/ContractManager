/// <reference path="~/GeneratedArtifacts/viewModel.js" />

myapp.BrowseProfitReportDatas.Excel_execute = function (screen) {
    // Write code here.
    var dataworkspace = new msls.application.DataWorkspace;
    var xls = dataworkspace.ApplicationData.ExcelExports.addNew();
    xls.TableName = "ProfitReport";

    dataworkspace.ApplicationData.saveChanges().then(
        function () {
            var documentBlob = xls.ExcelData;
            var blob = base64toBlob(documentBlob);
            var reportName = 'ProfitReport-' + new Date().toLocaleString() + '.xlsx';
            invokeSaveAsDialog(blob, reportName);
        }
    )

};