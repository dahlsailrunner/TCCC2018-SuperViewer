/// <reference path="../node_modules/@types/jquery/index.d.ts" />
/// <reference path="../node_modules/@types/kendo-ui/index.d.ts" />

module LogViewerModule {
    export class ViewModel extends kendo.data.ObservableObject {
        selectedEnvironment: string;
        availableEnvironments: string[];
        beginDt: Date;
        endDt: Date;
        availableMachines: string[];
        selectedMachines: string[] = [];
        availableUsers: string[];
        selectedUsers: string[] = [];
        availableLayers: string[];
        selectedLayers: string[] = [];
        likeTxt = "";
        notLikeTxt = "";
        limitTo = "100";
        logEntries: LogEntry[];
        correlationId = "";
        id = "";
        loadingMachines = false;
        loadingUsers = false;
        loadingLayers = false;
        includeInformational = false;
        informationalOnly = false;
        showCriteria = true;
        performingSearch = false;
        noEntries = false;
        noEnvironment = false;
        isError = false;

        constructor() {
            super();
            super.init(this);             

            $.ajax({
                url: '/api/LogsApi/Environments',
                method: "GET",
                dataType: "json"
            }).done(data => {                
                this.set("availableEnvironments", data);

                var env = CommonUtilities.getQueryStringParameterByName("env");
                if (env) {
                    this.set("selectedEnvironment", env);
                    this.loadCriteriaOptions();
                }
                else {
                    var lastEnvironment = localStorage.getItem("flogViewerEnv");
                    if (lastEnvironment) {
                        this.set("selectedEnvironment", lastEnvironment);
                        this.loadCriteriaOptions();
                    } else {
                        this.set("noEnvironment", true);
                    }
                }
                
                var today = new Date();
                this.set("beginDt", new Date(today.getFullYear(), today.getMonth(), today.getDate(), 0, 0, 0));                            

                var corrId = CommonUtilities.getQueryStringParameterByName("correlationId");
                if (corrId) {
                    this.set("correlationId", corrId);
                }
                var id = CommonUtilities.getQueryStringParameterByName("id");
                if (id) {
                    this.set("id", id);
                }
                if (this.correlationId !== "" || this.id !== "") {
                    this.toggleCriteria();
                    this.getLogEntries();
                }
            });
        }

        loadCriteriaOptions(): void {
            localStorage.setItem("flogViewerEnv", this.selectedEnvironment);
            this.set("noEnvironment", false);
            this.set("loadingUsers", true);
            this.set("loadingLayers", true);
            this.set("loadingMachines", true);
            this.set("isError", false);
            this.set("logEntries", []);        

            $.ajax({
                url: '/api/LogsApi/Options?env=' + this.selectedEnvironment + '&fieldname=Hostname',
                method: "GET",
                dataType: "json"
            }).done(data => {         
                this.set("loadingMachines", false);
                this.set("availableMachines", data);
                this.set("selectedMachines", ["ALL"]);
            }).fail((): void => {
                this.set("isError", true);
            });

            $.ajax({
                url: '/api/LogsApi/Options?env=' + this.selectedEnvironment + '&fieldname=UserName',
                method: "GET",
                dataType: "json"
            }).done(data => {
                this.set("loadingUsers", false);
                this.set("availableUsers", data);
                this.set("selectedUsers", ["ALL"]);
            }).fail((): void => {
                this.set("isError", true);
            });

            $.ajax({
                url: '/api/LogsApi/Options?env=' + this.selectedEnvironment + '&fieldname=Layer',
                method: "GET",
                dataType: "json"
            }).done(data => {
                this.set("loadingLayers", false);
                this.set("availableLayers", data);
                this.set("selectedLayers", ["ALL"]);    
            }).fail((): void => {
                this.set("isError", true);
                });                                   
        }

        toggleCriteria(): void {
            this.set("showCriteria", !this.showCriteria);
        }

        getLogEntries(): void {
            this.set("logEntries", []);
            this.set("performingSearch", true);
            this.set("isError", false);
            this.set("noEntries", false);

            var getUrl = "/api/LogsApi/Entries?env=" + this.selectedEnvironment
                + "&machineList=" + this.selectedMachines.join()
                + "&layerList=" + this.selectedLayers.join()
                + "&userList=" + this.selectedUsers.join()
                + "&beginDate=" + encodeURIComponent(CommonUtilities.getStringFromDate(this.beginDt))
                + "&endDate=" + (this.endDt ? encodeURIComponent(CommonUtilities.getStringFromDate(this.endDt)) : "")
                + "&correlationId=" + this.correlationId
                + "&id=" + this.id
                + "&like=" + encodeURIComponent(this.likeTxt)
                + "&notLike=" + encodeURIComponent(this.notLikeTxt)
                + "&limitTo=" + this.limitTo
                + "&includeInformational=" + this.includeInformational
                + "&informationalOnly=" + this.informationalOnly;
            
            $.ajax({
                url: getUrl,
                method: "GET",
                dataType: "json"
            }).done(data => {
                
                this.set("performingSearch", false);
                if (data && data.length > 0) {                    
                    this.set("logEntries", data);
                } else {
                    this.set("noEntries", true);
                } 

                this.set("id", "");
                this.set("correlationId", "");
            }).fail((): void => {
                this.set("isError", true);
            });
        }
    }
}
$(() => {
    var viewModel = new LogViewerModule.ViewModel();
    kendo.bind($("#mainContainer"), viewModel);
});