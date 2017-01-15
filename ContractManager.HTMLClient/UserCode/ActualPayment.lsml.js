/// <reference path="~/GeneratedArtifacts/viewModel.js" />

myapp.ActualPayment.created = function (entity) {
    // Write code here.
    entity.ProjectId = -1;
    var currentDate = new Date();
    entity.PaymentDate = new Date(currentDate.getFullYear(), currentDate.getMonth(), currentDate.getDate());
    entity.PaymentType = "Cash";
    //myapp.activeDataWorkspace.ApplicationData.Currencies_GetGEL().execute().then(
    //    function (results) {
    //        entity.setCurrency(results.results[0]);
    //        }
    //)

};
