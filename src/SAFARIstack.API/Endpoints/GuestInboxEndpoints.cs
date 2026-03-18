using Microsoft.AspNetCore.Mvc;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.API.Endpoints;

public static class GuestInboxEndpoints
{
    public static void MapGuestInboxEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/messages")
            .WithTags("Guest Inbox")
            .RequireAuthorization();

        // Receive inbound message (webhook from channels)
        group.MapPost("/inbound", async (
            InboundMessageDto message,
            IGuestInboxService svc) =>
        {
            var result = await svc.ReceiveMessageAsync(message);
            return Results.Created($"/api/messages/{result.Id}", result);
        })
        .WithName("ReceiveInboundMessage")
        .WithOpenApi()
        .Produces<GuestMessageDto>(StatusCodes.Status201Created);

        // Staff reply to message
        group.MapPost("/{messageId:guid}/reply", async (
            Guid messageId,
            MessageReplyRequest request,
            IGuestInboxService svc) =>
        {
            var result = await svc.SendReplyAsync(messageId, request.Reply, request.StaffId);
            return Results.Ok(result);
        })
        .WithName("ReplyToMessage")
        .WithOpenApi()
        .Produces<GuestMessageDto>(StatusCodes.Status200OK);

        // Approve AI-suggested reply
        group.MapPost("/{messageId:guid}/ai/approve", async (
            Guid messageId,
            AiApproveRequest request,
            IGuestInboxService svc) =>
        {
            var result = await svc.ApproveAiReplyAsync(messageId, request.StaffId);
            return Results.Ok(result);
        })
        .WithName("ApproveAiReply")
        .WithOpenApi()
        .Produces<GuestMessageDto>(StatusCodes.Status200OK);

        // Edit and approve AI reply
        group.MapPost("/{messageId:guid}/ai/edit-approve", async (
            Guid messageId,
            AiEditApproveRequest request,
            IGuestInboxService svc) =>
        {
            var result = await svc.EditAndApproveAiReplyAsync(messageId, request.EditedReply, request.StaffId);
            return Results.Ok(result);
        })
        .WithName("EditAndApproveAiReply")
        .WithOpenApi()
        .Produces<GuestMessageDto>(StatusCodes.Status200OK);

        // Get conversations for property
        group.MapGet("/conversations/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] bool unreadOnly,
            IGuestInboxService svc) =>
        {
            var result = await svc.GetConversationsAsync(propertyId, unreadOnly);
            return Results.Ok(result);
        })
        .WithName("GetConversations")
        .WithOpenApi()
        .Produces<IEnumerable<ConversationDto>>(StatusCodes.Status200OK);

        // Get single conversation with messages
        group.MapGet("/conversations/detail/{conversationId:guid}", async (
            Guid conversationId,
            IGuestInboxService svc) =>
        {
            var result = await svc.GetConversationAsync(conversationId);
            return Results.Ok(result);
        })
        .WithName("GetConversation")
        .WithOpenApi()
        .Produces<ConversationDto>(StatusCodes.Status200OK);

        // Assign conversation to staff
        group.MapPost("/conversations/{conversationId:guid}/assign", async (
            Guid conversationId,
            ConversationAssignRequest request,
            IGuestInboxService svc) =>
        {
            await svc.AssignConversationAsync(conversationId, request.StaffId);
            return Results.NoContent();
        })
        .WithName("AssignConversation")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent);

        // Resolve conversation
        group.MapPost("/conversations/{conversationId:guid}/resolve", async (
            Guid conversationId,
            IGuestInboxService svc) =>
        {
            await svc.ResolveConversationAsync(conversationId);
            return Results.NoContent();
        })
        .WithName("ResolveConversation")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent);
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────
public record MessageReplyRequest(string Reply, Guid StaffId);
public record AiApproveRequest(Guid StaffId);
public record AiEditApproveRequest(string EditedReply, Guid StaffId);
public record ConversationAssignRequest(Guid StaffId);
