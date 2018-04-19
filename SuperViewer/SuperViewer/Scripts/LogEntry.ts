module LogViewerModule {
    export class LogEntry {

        constructor(public id: string,
            public env: string,
            public timestamp: Date,            
            public product: string,
            public layer: string,
            public location: string,
            public message: string,
            public hostname: string,
            public userId: string,
            public userName: string,
            public exception: string,
            public elapsedMilliseconds: number,
            public correlationId: string,
            public customException: string,
            public additionalInfo: string) {
        }
    }
}