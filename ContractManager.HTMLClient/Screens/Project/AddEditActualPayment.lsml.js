/// <reference path="~/GeneratedArtifacts/viewModel.js" />

myapp.AddEditActualPayment.beforeApplyChanges = function (screen) {
    // Write code here.
    if (screen.ActualPayment.Paid == undefined)
        screen.ActualPayment.Paid = 0;

    if (screen.ActualPayment.Reversal == undefined)
        screen.ActualPayment.Reversal = 0;

    if (screen.ActualPayment.Discount == undefined)
        screen.ActualPayment.Discount = 0;

    if (screen.ActualPayment.ExchangeDifference == undefined)
        screen.ActualPayment.ExchangeDifference = 0;

};
//myapp.AddEditActualPayment.ActualPayment_Currency_postRender = function (element, contentItem) {
//    // Write code here.
//    contentItem.dataBind("value", function (value) {
//        if (value && value != contentItem.screen.ActualPayment.Currency) {

//            var contentItemCurrency = contentItem.screen.findContentItem("ActualPayment_Currency");
//            if (value.Id != contentItem.screen.gelId && value.Id != contentItem.screen.ActualPayment.Project.Currency.Id)
//                contentItemCurrency.validationResults =
//                    [new msls.ValidationResult(contentItem.screen.ActualPayment.details.properties.Currency, "არასწორი ვალუტა!")];
//            else
//                contentItemCurrency.validationResults = null;

//            if (contentItem.screen.ActualPayment.Project.Currency.Id == value.Id) {
//                contentItem.screen.findContentItem("ActualPayment_PaidGel").isVisible = false;
//                contentItem.screen.findContentItem("ActualPayment_ExchangeDifference").isVisible = false;;
//                contentItem.screen.findContentItem("ActualPayment_CurrencyRate").isVisible = false;

//            }
//            else {
//                if (contentItem.screen.ActualPayment.Project.Currency.Id == contentItem.screen.gelId) {

//                    myapp.activeDataWorkspace.ApplicationData.CurrencyRates_GetCurrencyRate(value.Id, contentItem.screen.ActualPayment.PaymentDate).execute().then(
//                        function (results) {
//                            contentItem.screen.ActualPayment.CurrencyRate = results.results[0].Rate;

//                        },

//                        function (error) {
//                            contentItem.screen.ActualPayment.CurrencyRate = 1;
//                        }
//                    )
//                }
//                contentItem.screen.findContentItem("ActualPayment_PaidGel").isVisible = true;
//                contentItem.screen.findContentItem("ActualPayment_ExchangeDifference").isVisible = true;;
//                contentItem.screen.findContentItem("ActualPayment_CurrencyRate").isVisible = true;

//            }

//        }
//        else contentItem.screen.ActualPayment.CurrencyRate = 1;
//    });
//};
myapp.AddEditActualPayment.PaymentDate_postRender = function (element, contentItem) {
    // Write code here.
    contentItem.dataBind("value", function (value) {
        if (contentItem.screen.gelId) {
            if (value && contentItem.screen.gelId && contentItem.screen.ActualPayment.Project.Currency.Id != contentItem.screen.gelId) {
                myapp.activeDataWorkspace.ApplicationData.CurrencyRates_GetCurrencyRate(contentItem.screen.ActualPayment.Project.Currency.Id, value).execute().then(
                    function (results) {
                        var curRate = results.results[0].Rate;
                        if (contentItem.screen.ActualPayment.CurrencyRate != curRate)
                            contentItem.screen.ActualPayment.CurrencyRate = curRate;
                    },

                    function (error) {
                        if (contentItem.screen.ActualPayment.CurrencyRate != 1)
                            contentItem.screen.ActualPayment.CurrencyRate = 1;
                    }
                )
            }
            else if (contentItem.screen.ActualPayment.CurrencyRate != 1)
                contentItem.screen.ActualPayment.CurrencyRate = 1;
        }
    });

};
myapp.AddEditActualPayment.created = function (screen) {
    // Write code here.
    screen.findContentItem("Paid").displayName = "თანხა " + screen.ActualPayment.Project.Currency.Name;
    screen.findContentItem("Reversal").displayName  = "დაბრუნება " + screen.ActualPayment.Project.Currency.Name;
    screen.findContentItem("Discount").displayName = "ფასდაკლება " + screen.ActualPayment.Project.Currency.Name;

    myapp.activeDataWorkspace.ApplicationData.Currencies_GetGEL().execute().then(
        function (results) {
            screen.gelId = results.results[0].Id;
            if (!screen.ActualPayment.Id)
                screen.ActualPayment.Currency = results.results[0];

            if (screen.ActualPayment.Project.Currency.Id != screen.gelId) {
                if (!screen.ActualPayment.Id)
                    myapp.activeDataWorkspace.ApplicationData.CurrencyRates_GetCurrencyRate(screen.ActualPayment.Project.Currency.Id, screen.ActualPayment.PaymentDate).execute().then(
                        function (results) {
                            screen.ActualPayment.CurrencyRate = results.results[0].Rate;
                        },

                        function (error) {
                            screen.ActualPayment.CurrencyRate = 1;
                        }
                    )
            }
            else {
                screen.findContentItem("ActualPayment_CurrencyRate").isVisible = false;
                screen.findContentItem("Paid").isVisible = false;
                screen.findContentItem("ActualPayment_ExchangeDifference").isVisible = false;;
                if (!screen.ActualPayment.Id)
                    screen.ActualPayment.CurrencyRate = 1;
            }

        }
    )

};
myapp.AddEditActualPayment.ActualPayment_PaidGel_postRender = function (element, contentItem) {
    // Write code here.
    
    contentItem.dataBind("value", function (value) {
        if (contentItem.screen.gelId) {
            if (value && contentItem.screen.ActualPayment.CurrencyRate != 0 && contentItem.screen.gelId && contentItem.screen.ActualPayment.Project.Currency.Id != contentItem.screen.gelId) {
                var paid = Math.round(value / contentItem.screen.ActualPayment.CurrencyRate * 100) / 100;
                if (contentItem.screen.ActualPayment.Paid != paid)
                    contentItem.screen.ActualPayment.Paid = paid;
            }
            else if (contentItem.screen.ActualPayment.Paid != value)
                contentItem.screen.ActualPayment.Paid = value;
        }
    });
};

myapp.AddEditActualPayment.ActualPayment_CurrencyRate_postRender = function (element, contentItem) {
    // Write code here.
    contentItem.dataBind("value", function (value) {
        if (contentItem.screen.gelId) {
            if (value && value != 0 && contentItem.screen.gelId && contentItem.screen.ActualPayment.Project.Currency.Id != contentItem.screen.gelId) {
                if (contentItem.screen.ActualPayment.PaidGel) {
                    var paid = Math.round(contentItem.screen.ActualPayment.PaidGel / value * 100) / 100;
                    if (contentItem.screen.ActualPayment.Paid != paid)
                        contentItem.screen.ActualPayment.Paid = paid;
                }
                else {
                    contentItem.screen.ActualPayment.Paid = 0;
                }
            }
            else if (contentItem.screen.ActualPayment.Paid != contentItem.screen.ActualPayment.PaidGel) {
                contentItem.screen.ActualPayment.Paid = contentItem.screen.ActualPayment.PaidGel;
            }
        }
    });
};