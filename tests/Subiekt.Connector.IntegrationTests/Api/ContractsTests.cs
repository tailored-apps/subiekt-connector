using System.Text.Json;
using FluentAssertions;
using Subiekt.Connector.Contracts;
using Xunit;

namespace Subiekt.Connector.IntegrationTests.Api;

public class ContractsTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ClientDto_DeserializesFromCamelCaseJson()
    {
        var json = """
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "name": "Testowa Firma Sp. z o.o.",
            "kind": "Company",
            "tin": "1234567890",
            "tinKind": "Nip",
            "favourite": true,
            "address": {
                "country": "PL",
                "city": "Warszawa",
                "zipCode": "00-001",
                "line1": "ul. Testowa 1"
            }
        }
        """;

        var client = JsonSerializer.Deserialize<ClientDto>(json, JsonOpts);

        client.Should().NotBeNull();
        client!.Name.Should().Be("Testowa Firma Sp. z o.o.");
        client.Kind.Should().Be(ClientKind.Company);
        client.Tin.Should().Be("1234567890");
        client.Address?.City.Should().Be("Warszawa");
    }

    [Fact]
    public void DocumentListDto_DeserializesCorrectly()
    {
        var json = """
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "documentNumber": "FV/2024/001",
            "kind": "Invoice",
            "issueDate": "2024-01-15T00:00:00",
            "dueDate": "2024-01-29T00:00:00",
            "totalGross": 1230.00,
            "totalNet": 1000.00,
            "paymentState": "Paid",
            "currency": "PLN"
        }
        """;

        var doc = JsonSerializer.Deserialize<DocumentListDto>(json, JsonOpts);

        doc.Should().NotBeNull();
        doc!.DocumentNumber.Should().Be("FV/2024/001");
        doc.Kind.Should().Be(DocumentKind.Invoice);
        doc.TotalGross.Should().Be(1230.00m);
        doc.PaymentState.Should().Be(PaymentState.Paid);
    }
}
