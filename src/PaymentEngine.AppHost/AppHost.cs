var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var paymentDb = postgres.AddDatabase("payments");

builder.AddProject<Projects.PaymentEngine_Api>("api")
    .WithReference(paymentDb)
    .WaitFor(paymentDb);

builder.Build().Run();
