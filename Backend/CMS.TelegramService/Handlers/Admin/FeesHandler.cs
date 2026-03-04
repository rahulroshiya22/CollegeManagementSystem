using System.Text;
using System.Text.Json;
using CMS.TelegramService.Services;
using CMS.TelegramService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;

namespace CMS.TelegramService.Handlers.Admin;

public class FeesHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly SessionService _sessions;
    private readonly ApiService _api;

    public FeesHandler(ITelegramBotClient bot, SessionService sessions, ApiService api)
    { _bot = bot; _sessions = sessions; _api = api; }

    public async Task HandleCallback(CallbackQuery query)
    {
        var data = query.Data ?? "";
        var userId = query.From.Id;

        if (data == "admin_fees") { await _bot.AnswerCallbackQuery(query.Id); await ListFeesDashboard(query); return; }
        if (data.StartsWith("admin_fees_filter_")) { await ViewFilteredFees(query); return; }
        if (data.StartsWith("view_fee_")) { await ViewFeeDetail(query, data.Replace("view_fee_", "")); return; }
        if (data == "search_fee_start") { await SearchFeeStart(query, userId); return; }
        if (data == "add_fee_start") { await AddFeeStart(query, userId); return; }
        
        if (data.StartsWith("pay_fee_")) { await MarkFeePaid(query, data.Replace("pay_fee_", "")); return; }
        if (data.StartsWith("remind_fee_")) { await _bot.AnswerCallbackQuery(query.Id, "🔔 Reminder sent! (Simulated)", showAlert: true); return; }
        if (data.StartsWith("receipt_fee_")) { await DownloadReceipt(query, data.Replace("receipt_fee_", "")); return; }
        
        if (data.StartsWith("delete_fee_")) { await DeleteFeeConfirm(query, data.Replace("delete_fee_", "")); return; }
        if (data.StartsWith("confirm_del_fee_")) { await DeleteFee(query, data.Replace("confirm_del_fee_", "")); return; }
    }

    private async Task<Dictionary<string, string>> GetStudentMap(long userId)
    {
        var map = new Dictionary<string, string>();
        var (resp, _) = await _api.GetAsync(userId, "/api/student?Page=1&PageSize=1000");
        
        var students = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;
        if (resp?.ValueKind == JsonValueKind.Object && resp.Value.TryGetProperty("data", out var d)) students = d;
        
        if (students.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in students.EnumerateArray())
            {
                var sid = s.Str("studentId", s.Str("id"));
                var name = $"{s.Str("firstName")} {s.Str("lastName")}".Trim();
                if (!string.IsNullOrEmpty(sid)) map[sid] = name;
            }
        }
        return map;
    }

    private async Task ListFeesDashboard(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (resp, err) = await _api.GetAsync(userId, "/api/fee");
        
        if (err != null)
        {
            await _bot.EditMessageText(chatId, query.Message!.MessageId, $"❌ Error: {err}", replyMarkup: MenuHandler.BackButton());
            return;
        }

        var fees = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;
        if (resp?.ValueKind == JsonValueKind.Object && resp.Value.TryGetProperty("data", out var d)) fees = d;

        decimal totalCollected = 0, totalPending = 0;
        if (fees.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in fees.EnumerateArray())
            {
                bool isPaid = f.Str("status") == "Paid" || f.Bool("isPaid");
                var amt = f.Dec("amount");
                if (isPaid) totalCollected += amt;
                else totalPending += amt;
            }
        }

        var text = $"💰 <b>Fee Management Dashboard</b>\n━━━━━━━━━━━━━━━━━━━━\n" +
                   $"✅ <b>Collected:</b> {FormattingUtils.FormatCurrency(totalCollected)}\n⏳ <b>Pending:</b> {FormattingUtils.FormatCurrency(totalPending)}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━\nSelect an action:";

        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("📜 View Pending Fees", "admin_fees_filter_pending_page_1") },
            new[] { InlineKeyboardButton.WithCallbackData("✅ View Paid History", "admin_fees_filter_paid_page_1") },
            new[] { InlineKeyboardButton.WithCallbackData("🔍 Search Student Fee", "search_fee_start") },
            new[] { InlineKeyboardButton.WithCallbackData("➕ Create New Fee", "add_fee_start") },
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Main Menu", "main_menu") }
        });

        try { await query.Message!.Delete(_bot); } catch { }
        await _bot.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task ViewFilteredFees(CallbackQuery query)
    {
        var parts = query.Data!.Split('_');
        var statusFilter = parts[3];
        int page = int.Parse(parts[5]);

        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (resp, _) = await _api.GetAsync(userId, "/api/fee");
        var fees = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;
        if (resp?.ValueKind == JsonValueKind.Object && resp.Value.TryGetProperty("data", out var d)) fees = d;

        var filtered = new List<JsonElement>();
        if (fees.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in fees.EnumerateArray())
            {
                bool isPaid = f.Str("status") == "Paid" || f.Bool("isPaid");
                if (statusFilter == "pending" && !isPaid) filtered.Add(f);
                else if (statusFilter == "paid" && isPaid) filtered.Add(f);
            }
        }

        var studentMap = await GetStudentMap(userId);
        
        int itemsPerPage = 5;
        int totalPages = Math.Max(1, (filtered.Count + itemsPerPage - 1) / itemsPerPage);
        var currentItems = filtered.Skip((page - 1) * itemsPerPage).Take(itemsPerPage).ToList();

        string title = statusFilter == "pending" ? $"⏳ <b>Pending Fees</b>" : $"✅ <b>Paid History</b>";
        string text = $"{title} (Page {page}/{totalPages})\n━━━━━━━━━━━━━━━━━━━━\n\n";

        if (currentItems.Count == 0) text += "<i>No records found.</i>\n";

        var kb = new List<InlineKeyboardButton[]>();
        foreach (var f in currentItems)
        {
            var fid = f.Str("feeId", f.Str("id"));
            var sid = f.Str("studentId", "?");
            var sName = studentMap.TryGetValue(sid, out var n) ? n : $"Student #{sid}";
            var amt = FormattingUtils.FormatCurrency(f.Dec("amount"));
            var desc = f.Str("description", "Fee");

            text += $"👤 <b>{sName}</b>\n   💵 {amt} - {desc}\n\n";
            kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"👁️ View {sName.Split()[0]}", $"view_fee_{fid}") });
        }

        var nav = new List<InlineKeyboardButton>();
        if (page > 1) nav.Add(InlineKeyboardButton.WithCallbackData("⬅️ Prev", $"admin_fees_filter_{statusFilter}_page_{page - 1}"));
        if (page < totalPages) nav.Add(InlineKeyboardButton.WithCallbackData("Next ➡️", $"admin_fees_filter_{statusFilter}_page_{page + 1}"));
        if (nav.Count > 0) kb.Add(nav.ToArray());

        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Dashboard", "admin_fees") });
        await _bot.EditMessageText(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task ViewFeeDetail(CallbackQuery query, string fid)
    {
        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (resp, err) = await _api.GetAsync(userId, $"/api/fee/{fid}");
        
        if (err != null)
        {
            await _bot.EditMessageText(chatId, query.Message!.MessageId, "❌ Fee Record not found.", replyMarkup: MenuHandler.BackButton("admin_fees"));
            return;
        }

        var fee = resp!.Value;
        var sid = fee.Str("studentId");
        var (sResp, _) = await _api.GetAsync(userId, $"/api/student/{sid}");
        var sName = sResp?.ValueKind == JsonValueKind.Object ? $"{sResp.Value.Str("firstName")} {sResp.Value.Str("lastName")}".Trim() : $"Student #{sid}";

        var amt = FormattingUtils.FormatCurrency(fee.Dec("amount"));
        var desc = fee.Str("description", "N/A");
        var due = fee.Str("dueDate", "N/A").Split('T')[0];
        var status = fee.Str("status", fee.Bool("isPaid") ? "Paid" : "Pending");
        bool isPaid = status == "Paid";
        var icon = isPaid ? "✅" : "⏳";

        var text = $"🏠 Home > 💰 Fees > 🧾 <b>Details</b>\n\n🧾 <b>FEE RECEIPT</b>\n━━━━━━━━━━━━━━━━━━━━\n" +
                   $"🎓 <b>Student:</b> {sName}\n💵 <b>Amount:</b> {amt}\n📅 <b>Due Date:</b> {due}\n📝 <b>Description:</b> {desc}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━\n<b>Status:</b> {icon} {status}\n";

        var kb = new List<InlineKeyboardButton[]>();
        if (!isPaid)
        {
            kb.Add(new[] { InlineKeyboardButton.WithCallbackData("💳 Mark as Paid", $"pay_fee_{fid}") });
            kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔔 Send Reminder", $"remind_fee_{fid}") });
        }
        else
        {
            kb.Add(new[] { InlineKeyboardButton.WithCallbackData("📥 Download Receipt", $"receipt_fee_{fid}") });
        }
        
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🗑️ Delete Fee", $"delete_fee_{fid}") });
        kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Back to Dashboard", "admin_fees") });

        await _bot.EditMessageText(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
    }

    private async Task DeleteFeeConfirm(CallbackQuery query, string fid)
    {
        var chatId = query.Message!.Chat.Id;
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🗑️ Yes, Delete Forever", $"confirm_del_fee_{fid}") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Cancel", $"view_fee_{fid}") }
        });

        var text = $"⚠️ <b>Delete Fee Record?</b>\n\nAre you sure you want to delete this fee record?\nThis action <b>cannot</b> be undone.";
        await _bot.EditMessageText(chatId, query.Message!.MessageId, text, parseMode: ParseMode.Html, replyMarkup: kb);
    }

    private async Task DeleteFee(CallbackQuery query, string fid)
    {
        var userId = query.From.Id;
        var (_, err) = await _api.DeleteAsync(userId, $"/api/fee/{fid}");
        
        if (err == null)
        {
            await _bot.AnswerCallbackQuery(query.Id, "Fee Deleted!", showAlert: true);
            await ListFeesDashboard(query);
        }
        else
        {
            await _bot.AnswerCallbackQuery(query.Id, $"Failed: {err}", showAlert: true);
            query.Data = $"view_fee_{fid}";
            await ViewFeeDetail(query, fid);
        }
    }

    private async Task MarkFeePaid(CallbackQuery query, string fid)
    {
        var userId = query.From.Id;
        var (resp, err) = await _api.PostAsync(userId, $"/api/fee/{fid}/pay", new { });
        
        if (err == null)
        {
            await _bot.AnswerCallbackQuery(query.Id, "✅ Fee Marked as Paid!", showAlert: true);
            query.Data = $"view_fee_{fid}";
            await ViewFeeDetail(query, fid);
        }
        else
        {
            await _bot.AnswerCallbackQuery(query.Id, $"Failed: {err}", showAlert: true);
        }
    }

    private async Task DownloadReceipt(CallbackQuery query, string fid)
    {
        await _bot.AnswerCallbackQuery(query.Id, "Generating Receipt...");

        var chatId = query.Message!.Chat.Id; var userId = query.From.Id;
        var (resp, _) = await _api.GetAsync(userId, $"/api/fee/{fid}");
        if (resp == null) return;

        var fee = resp.Value;
        var sid = fee.Str("studentId");
        var (sResp, _) = await _api.GetAsync(userId, $"/api/student/{sid}");
        var sName = sResp?.ValueKind == JsonValueKind.Object ? $"{sResp.Value.Str("firstName")} {sResp.Value.Str("lastName")}".Trim() : $"Student #{sid}";

        var receiptNo = $"REC-{int.Parse(fid):D6}";
        var paidDate = fee.Str("paidDate");
        var datePaid = string.IsNullOrEmpty(paidDate) ? DateTime.Now.ToString("yyyy-MM-dd") : paidDate.Split('T')[0];
        var amt = FormattingUtils.FormatCurrency(fee.Dec("amount"));
        var desc = fee.Str("description", "Tuition Fee");

        // Generate HTML Receipt instead of PDF to avoid dependency requirement
        string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
<style>
    body {{ font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; margin: 0; padding: 20px; color: #333; }}
    .header {{ background-color: #1E3A8A; color: white; padding: 20px; text-align: left; display: flex; justify-content: space-between; }}
    .header h1 {{ margin: 0; font-size: 24px; }}
    .header p {{ margin: 5px 0 0 0; font-size: 14px; opacity: 0.9; }}
    .receipt-info {{ text-align: right; }}
    .student-details {{ margin-top: 30px; margin-bottom: 20px; }}
    table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
    th, td {{ padding: 12px 15px; border-bottom: 1px solid #E5E7EB; text-align: left; }}
    th {{ background-color: #E5E7EB; font-weight: bold; }}
    .total-row td {{ font-weight: bold; font-size: 18px; }}
    .total-amt {{ color: #059669; text-align: right; }}
    .stamp {{ color: #059669; border: 3px solid #059669; padding: 10px 20px; border-radius: 10px; font-size: 32px; font-weight: bold; transform: rotate(-15deg); display: inline-block; position: absolute; right: 50px; bottom: 100px; opacity: 0.8; background-color: #DEF7EC; }}
    .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 50px; }}
</style>
</head>
<body>
    <div class='header'>
        <div>
            <h1>COLLEGE MANAGEMENT SYSTEM</h1>
            <p>Official Payment Receipt</p>
        </div>
        <div class='receipt-info'>
            <h2>{receiptNo}</h2>
            <p>Date: {datePaid}</p>
        </div>
    </div>
    <div class='student-details'>
        <h3>Received From:</h3>
        <p>Student Name: <strong>{sName}</strong></p>
        <p>Student ID: {sid}</p>
    </div>
    <table>
        <tr><th>Description</th><th style='text-align: right;'>Amount</th></tr>
        <tr><td>{desc}</td><td style='text-align: right;'>{amt}</td></tr>
        <tr class='total-row'>
            <td style='text-align: right;'>Total Paid:</td>
            <td class='total-amt'>{amt}</td>
        </tr>
    </table>
    <div class='stamp'>PAID</div>
    <div class='footer'>
        <p>This is a computer-generated receipt and requires no signature.</p>
        <p>Thank you for your business.</p>
    </div>
</body>
</html>";

        var filename = $"Receipt_{fid}.html";
        await System.IO.File.WriteAllTextAsync(filename, htmlContent);

        await using var stream = System.IO.File.OpenRead(filename);
        await _bot.SendDocument(chatId, InputFile.FromStream(stream, filename), caption: $"🧾 <b>Official Receipt - {receiptNo}</b>", parseMode: ParseMode.Html);
        
        System.IO.File.Delete(filename);
    }

    private async Task SearchFeeStart(CallbackQuery query, long userId)
    {
        try { await query.Message!.Delete(_bot); } catch { }
        _sessions.SetState(userId, "search_fee");
        await _bot.SendMessage(query.Message!.Chat.Id, $"🔍 <b>Search Fee</b>\n\nEnter Student Name:", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_fees"));
    }

    private async Task AddFeeStart(CallbackQuery query, long userId)
    {
        try { await query.Message!.Delete(_bot); } catch { }
        _sessions.SetState(userId, "f_add_student");
        await _bot.SendMessage(query.Message!.Chat.Id, $"➕ <b>Create Fee</b>\n\nEnter <b>Student ID</b>:", parseMode: ParseMode.Html, replyMarkup: MenuHandler.BackButton("admin_fees"));
    }

    public async Task HandleState(Message msg, string state)
    {
        var userId = msg.From!.Id; var chatId = msg.Chat.Id; var text = msg.Text ?? "";
        
        if (state == "search_fee")
        {
            _sessions.ClearState(userId);
            var q = text.ToLower();
            var (resp, _) = await _api.GetAsync(userId, "/api/fee");
            var fees = resp?.ValueKind == JsonValueKind.Array ? resp.Value : default;
            if (resp?.ValueKind == JsonValueKind.Object && resp.Value.TryGetProperty("data", out var d)) fees = d;

            var studentMap = await GetStudentMap(userId);
            var matches = new List<JsonElement>();

            if (fees.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in fees.EnumerateArray())
                {
                    var sid = f.Str("studentId", "");
                    var sName = studentMap.TryGetValue(sid, out var n) ? n : "";
                    if (sName.ToLower().Contains(q)) matches.Add(f);
                }
            }

            if (matches.Count == 0)
            {
                await _bot.SendMessage(chatId, "❌ No records found.", replyMarkup: MenuHandler.BackButton("admin_fees"));
                return;
            }

            var resText = $"🔍 <b>Results for \"{text}\"</b>\n━━━━━━━━━━━━━━━━━━━━\n";
            var kb = new List<InlineKeyboardButton[]>();

            foreach (var f in matches.Take(5))
            {
                var fid = f.Str("feeId", f.Str("id"));
                var sid = f.Str("studentId");
                var sName = studentMap[sid];
                var amt = FormattingUtils.FormatCurrency(f.Dec("amount"));
                
                resText += $"• {sName}: {amt}\n";
                kb.Add(new[] { InlineKeyboardButton.WithCallbackData($"View {sName}", $"view_fee_{fid}") });
            }

            kb.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Dashboard", "admin_fees") });
            await _bot.SendMessage(chatId, resText, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(kb));
        }
        else if (state == "f_add_student")
        {
            _sessions.SetData(userId, "new_fee_sid", text);
            _sessions.SetState(userId, "f_add_amount");
            await _bot.SendMessage(chatId, "Enter <b>Amount</b> (e.g., 5000):", parseMode: ParseMode.Html);
        }
        else if (state == "f_add_amount")
        {
            _sessions.SetData(userId, "new_fee_amt", text);
            _sessions.SetState(userId, "f_add_desc");
            await _bot.SendMessage(chatId, "Enter <b>Description</b> (e.g., Semester 1 Fee):", parseMode: ParseMode.Html);
        }
        else if (state == "f_add_desc")
        {
            _sessions.SetData(userId, "new_fee_desc", text);
            _sessions.SetState(userId, "f_add_date");
            await _bot.SendMessage(chatId, "Enter <b>Due Date</b> (YYYY-MM-DD):", parseMode: ParseMode.Html);
        }
        else if (state == "f_add_date")
        {
            _sessions.ClearState(userId);
            var payload = new 
            {
                studentId = int.TryParse(_sessions.GetData<string>(userId, "new_fee_sid"), out var s) ? s : 0,
                amount = float.TryParse(_sessions.GetData<string>(userId, "new_fee_amt"), out var a) ? a : 0,
                description = _sessions.GetData<string>(userId, "new_fee_desc"),
                dueDate = $"{text}T00:00:00Z"
            };

            var (resp, err) = await _api.PostAsync(userId, "/api/fee", payload);
            if (err == null) await _bot.SendMessage(chatId, "✅ Fee Record Created!", replyMarkup: MenuHandler.BackButton("admin_fees"));
            else await _bot.SendMessage(chatId, $"❌ Failed: {err}", replyMarkup: MenuHandler.BackButton("admin_fees"));
        }
    }
}
