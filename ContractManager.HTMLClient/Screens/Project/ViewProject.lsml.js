/// <reference path="~/GeneratedArtifacts/viewModel.js" />

myapp.ViewProject.Details_postRender = function (element, contentItem) {
    // Write code here.
    var name = contentItem.screen.Project.details.getModel()[':@SummaryProperty'].property.name;
    contentItem.dataBind("screen.Project." + name, function (value) {
        contentItem.screen.details.displayName = value;
    });
};
myapp.ViewProject.UploadFile_execute = function (screen) {
    // Write code here.

};
myapp.ViewProject.Recalculate_execute = function (screen) {

    screen.PaymentSchedules.refresh();

};
myapp.ViewProject.deleteSelected_execute = function (screen) {
    screen.ActualPayments.deleteSelected();
    return myapp.activeDataWorkspace.ApplicationData.saveChanges().then(function success() {
        // If success.
        myapp.ViewProject.Recalculate_execute(screen);
        }, function fail(e) {
        throw e;
        });
};
myapp.ViewProject.deleteSelected_canExecute = function (screen) {
    return screen.ActualPayments.selectedItem !== null;
};
myapp.ViewProject.Print_execute = function (screen) {


};


myapp.ViewProject.deletePaymentSchedule_execute = function (screen) {
    // Write code here.
    screen.PaymentSchedules.deleteSelected();
    return myapp.activeDataWorkspace.ApplicationData.saveChanges().then(function success() {
        // If success.
        myapp.ViewProject.Recalculate_execute(screen);
    }, function fail(e) {
        throw e;
    });

};

//myapp.ViewProject.Total_render = function (element, contentItem) {    
//    var $normalParagraph = $('<p style="text-align:right;" ><b>სულ ' + contentItem.screen.Total + '</b></p>');
//    $(element).append($normalParagraph);
//};
// Function to compute the total for the Order 
function TotalOfStages(ProjectStages) {
    // Start with 0
    var TotalAmount = 0;
    // Get the data for the collection passed
    var ProjectStage = ProjectStages.data;
    // Loop through each row
    ProjectStage.forEach(function (stage) {
        // Add each row to TotalAmountOfOrders
        TotalAmount += parseFloat(stage.StageTotal);
    });
    // Return TotalAmountOfOrders
    return TotalAmount;
}

function TotalOfPaymentDue(PaymentSchedules) {
    // Start with 0
    var TotalAmount = 0;
    // Get the data for the collection passed
    var PaymentSchedule = PaymentSchedules.data;
    // Loop through each row
    PaymentSchedule.forEach(function (Schedule) {
        // Add each row to TotalAmountOfOrders
        TotalAmount += parseFloat(Schedule.TotalDue);
    });
    // Return TotalAmountOfOrders
    return TotalAmount;
}

function TotalPaid(ActualPayments) {
    // Start with 0
    var TotalAmount = 0;
    // Get the data for the collection passed
    var ActualPayment = ActualPayments.data;
    // Loop through each row
    ActualPayment.forEach(function (Payment) {
        // Add each row to TotalAmountOfOrders
        TotalAmount += parseFloat(Payment.Paid) + parseFloat(Payment.Reversal) + parseFloat(Payment.Discount);
    });
    // Return TotalAmountOfOrders
    return Math.round(TotalAmount*100)/100;
}

myapp.ViewProject.created = function (screen) {
    // Update total of stages
    function updateTotal() {
        var totalProject = TotalOfStages(screen.ProjectStages);
        if (screen.Total != totalProject) {
            screen.Total = totalProject;
            screen.ProjectStages.refresh();
        }            
    }

    // Set a dataBind to update the value when the collection changes
    var contentItem = screen.findContentItem("Total");
    
    contentItem.dataBind("screen.ProjectStages.count", updateTotal);
    contentItem.dataBind("screen.ProjectStages.selectedItem.StageTotal", updateTotal);


    // Update total payment due
    function updatePaymentDue(value) {
        var totalDue = TotalOfPaymentDue(screen.PaymentSchedules);
        if (screen.TotalDue != totalDue) {
            screen.TotalDue = totalDue;
            screen.PaymentSchedules.refresh();
        }
            
    }

    // Set a dataBind to update the value when the collection changes
    var contentItem = screen.findContentItem("TotalDue1");

    contentItem.dataBind("screen.PaymentSchedules.count", updatePaymentDue);
    contentItem.dataBind("screen.PaymentSchedules.selectedItem.TotalDue", updatePaymentDue);


    // Update total paid
    function updateTotalPaid() {
        var totalPaid = TotalPaid(screen.ActualPayments);

        if (screen.TotalPaid != totalPaid) {
            screen.TotalPaid = totalPaid;
            screen.PaymentSchedules.refresh();
            screen.ProjectStages.refresh();
            screen.ActualPayments.refresh();
        }
    }

    // Set a dataBind to update the value when the collection changes
    var contentItem = screen.findContentItem("TotalPaid");

    contentItem.dataBind("screen.ActualPayments.count", updateTotalPaid);
    contentItem.dataBind("screen.ActualPayments.selectedItem.Paid", updateTotalPaid);
    contentItem.dataBind("screen.ActualPayments.selectedItem.Reversal", updateTotalPaid);
    contentItem.dataBind("screen.ActualPayments.selectedItem.Discount", updateTotalPaid);

};

myapp.ViewProject.Contract_execute = function (screen) {
    var dataworkspace = new msls.application.DataWorkspace;
    var doc = dataworkspace.ApplicationData.PrintDocuments.addNew();
    doc.ProjectId = screen.Project.Id;
    doc.DocumentName = "Contract";

    dataworkspace.ApplicationData.saveChanges().then(
        function () {
            var documentBlob = doc.DocumentData;
            var blob = base64toBlob(documentBlob);
            invokeSaveAsDialog(blob, 'Contract-' + screen.Project.ContractNumber + '.docx');
        }
    )

    screen.closePopup();

};
myapp.ViewProject.Acceptance_execute = function (screen) {
    var dataworkspace = new msls.application.DataWorkspace;
    var doc = dataworkspace.ApplicationData.PrintDocuments.addNew();
    doc.ProjectId = screen.Project.Id;
    doc.DocumentName = "Acceptance";

    dataworkspace.ApplicationData.saveChanges().then(
        function () {
            var documentBlob = doc.DocumentData;
            var blob = base64toBlob(documentBlob);
            invokeSaveAsDialog(blob, 'Acceptance-' + screen.Project.ContractNumber + '.docx');
        }
    )

    screen.closePopup();

};