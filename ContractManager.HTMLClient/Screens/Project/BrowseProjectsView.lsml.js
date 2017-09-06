/// <reference path="~/GeneratedArtifacts/viewModel.js" />

myapp.BrowseProjectsView.rows_postRender = function (element, contentItem) {
    // Write code here.
    contentItem.dataBind("value.AlertType", function (value) {
        if (value > 0) {
            $(element).css("background", "#F5858B");
        }
    });
};
myapp.BrowseProjectsView.RunDebtsReport_execute = function (screen) {
    // Write code here.
    
};
myapp.BrowseProjectsView.DebtsReport_Tap_execute = function (screen) {
    // Write code here.
    myapp.activeDataWorkspace.ApplicationData.DebtReportCommands.addNew();
    myapp.activeDataWorkspace.ApplicationData.saveChanges().then(
        function success() {
            // If success.
            msls.application.showScreen("BrowseDebtReportDatas");
        }, function fail(e) {
            throw e;
        });
 
};
myapp.BrowseProjectsView.ProfitReportRun_execute = function (screen) {
    // Write code here.
    var profitRep = myapp.activeDataWorkspace.ApplicationData.ProfitReportCommands.addNew();
    profitRep.StartDate = screen.StartDate;
    profitRep.EndDate = screen.EndDate;
    myapp.activeDataWorkspace.ApplicationData.saveChanges().then(
        function success() {
            // If success.
            msls.application.showScreen("BrowseProfitReportDatas");
        }, function fail(e) {
            throw e;
        });

};
myapp.BrowseProjectsView.exportExcel_execute = function (screen) {
    // Write code here.
    var dataworkspace = new msls.application.DataWorkspace;
    var xls = dataworkspace.ApplicationData.ExcelExports.addNew();
    xls.TableName = "ProjectList";

    dataworkspace.ApplicationData.saveChanges().then(
        function () {
            var documentBlob = xls.ExcelData;
            var blob = base64toBlob(documentBlob);
            var reportName = 'ProjectList-' + new Date().toLocaleString() + '.xlsx';
            invokeSaveAsDialog(blob, reportName);
        }
    )

};