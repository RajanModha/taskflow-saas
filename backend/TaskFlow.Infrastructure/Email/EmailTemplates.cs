namespace TaskFlow.Infrastructure.Email;

public static class EmailTemplates
{
    private static string Base(string content) => $"""
        <!DOCTYPE html><html><body style="font-family:-apple-system,BlinkMacSystemFont,
        'Segoe UI',sans-serif;background:#f4f4f5;margin:0;padding:40px 20px;">
          <div style="max-width:520px;margin:0 auto;background:#fff;border-radius:12px;
                      overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);">
            <div style="background:#18181b;padding:24px 32px;">
              <h1 style="color:#fff;margin:0;font-size:20px;font-weight:600;">TaskFlow</h1>
            </div>
            <div style="padding:32px;">
              {content}
              <hr style="border:none;border-top:1px solid #e4e4e7;margin:32px 0 24px;"/>
              <p style="color:#71717a;font-size:13px;margin:0;">
                If you didn't request this, you can safely ignore this email.
              </p>
            </div>
          </div>
        </body></html>
        """;

    public static string VerifyEmail(string userName, string verifyUrl) => Base($"""
        <h2 style="color:#18181b;margin:0 0 8px;">Verify your email</h2>
        <p style="color:#52525b;line-height:1.6;">Hi {userName}, thanks for signing up for
        TaskFlow! Please verify your email address to get started.</p>
        <a href="{verifyUrl}" style="display:inline-block;background:#18181b;color:#fff;
           padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:500;
           margin:24px 0;">Verify email address</a>
        <p style="color:#71717a;font-size:13px;">This link expires in 24 hours.</p>
        """);

    public static string ResetPassword(string userName, string resetUrl) => Base($"""
        <h2 style="color:#18181b;margin:0 0 8px;">Reset your password</h2>
        <p style="color:#52525b;line-height:1.6;">Hi {userName}, we received a request
        to reset your password for your TaskFlow account.</p>
        <a href="{resetUrl}" style="display:inline-block;background:#18181b;color:#fff;
           padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:500;
           margin:24px 0;">Reset password</a>
        <p style="color:#71717a;font-size:13px;">This link expires in 1 hour.</p>
        """);

    public static string WorkspaceInvite(string inviterName, string workspaceName, string joinUrl, string role) => Base($"""
        <h2 style="color:#18181b;margin:0 0 8px;">You're invited to {workspaceName}</h2>
        <p style="color:#52525b;line-height:1.6;"><strong>{inviterName}</strong> invited
        you to join <strong>{workspaceName}</strong> as a <strong>{role}</strong>.</p>
        <a href="{joinUrl}" style="display:inline-block;background:#18181b;color:#fff;
           padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:500;
           margin:24px 0;">Accept invite</a>
        <p style="color:#71717a;font-size:13px;">This invite expires in 7 days.</p>
        """);

    public static string DueDateReminder(
        string userName,
        string taskTitle,
        string projectName,
        string dueDate,
        string taskUrl) => Base($"""
        <h2 style="color:#18181b;margin:0 0 8px;">Task due soon</h2>
        <p style="color:#52525b;line-height:1.6;">Hi {userName}, a task assigned
        to you is due in less than 24 hours.</p>
        <div style="background:#fafafa;border:1px solid #e4e4e7;border-radius:8px;
                    padding:16px;margin:20px 0;">
          <p style="margin:0 0 4px;font-weight:600;color:#18181b;">{taskTitle}</p>
          <p style="margin:0;color:#71717a;font-size:14px;">Project: {projectName}</p>
          <p style="margin:4px 0 0;color:#ef4444;font-size:14px;">Due: {dueDate}</p>
        </div>
        <a href="{taskUrl}" style="display:inline-block;background:#18181b;color:#fff;
           padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:500;
           margin:4px 0;">View task</a>
        """);

    public static string WelcomeEmail(string userName, string workspaceName) => Base($"""
        <h2 style="color:#18181b;margin:0 0 8px;">Welcome to TaskFlow!</h2>
        <p style="color:#52525b;line-height:1.6;">Hi {userName}, your workspace
        <strong>{workspaceName}</strong> is ready. Start by creating your first project
        and inviting your team.</p>
        <div style="background:#f0fdf4;border:1px solid #bbf7d0;border-radius:8px;
                    padding:16px;margin:20px 0;">
          <p style="margin:0 0 6px;font-weight:600;color:#166534;">Quick start</p>
          <p style="margin:0;color:#166534;font-size:14px;line-height:1.8;">
            ✓ Create a project<br/>✓ Add tasks with priorities and due dates<br/>
            ✓ Invite teammates with your join code</p>
        </div>
        """);

    public static string TaskAssigned(
        string userName,
        string taskTitle,
        string projectName,
        string assignerName,
        string taskUrl) => Base($"""
        <h2 style="color:#18181b;margin:0 0 8px;">Task assigned to you</h2>
        <p style="color:#52525b;line-height:1.6;">Hi {userName},
        <strong>{assignerName}</strong> assigned a task to you in
        <strong>{projectName}</strong>.</p>
        <div style="background:#fafafa;border:1px solid #e4e4e7;border-radius:8px;
                    padding:16px;margin:20px 0;">
          <p style="margin:0;font-weight:600;color:#18181b;">{taskTitle}</p>
        </div>
        <a href="{taskUrl}" style="display:inline-block;background:#18181b;color:#fff;
           padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:500;
           margin:4px 0;">View task</a>
        """);

    public static string BulkTaskAssignedSummary(
        string userName,
        int count,
        string workspaceName,
        string tasksUrl) => Base($"""
        <h2 style="color:#18181b;margin:0 0 8px;">You have new task assignments</h2>
        <p style="color:#52525b;line-height:1.6;">Hi {userName}, you have been assigned
        <strong>{count}</strong> tasks in <strong>{workspaceName}</strong>.</p>
        <a href="{tasksUrl}" style="display:inline-block;background:#18181b;color:#fff;
           padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:500;
           margin:24px 0;">View tasks</a>
        """);
}

