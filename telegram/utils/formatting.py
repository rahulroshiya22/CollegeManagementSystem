from datetime import datetime
import random

def get_breadcrumbs(path_list):
    """
    Generates a breadcrumb string from a list of steps.
    Example: ['Home', 'Students', 'IT'] -> "🏠 Home > 🎓 Students > 📂 IT"
    """
    return " > ".join(path_list)

def get_progress_bar(value, total=100, length=10):
    """
    Generates a text-based progress bar.
    Example: [██████░░░░] 60%
    """
    if total == 0:
        return f"[{'░' * length}] 0%"
    
    percent = value / total
    filled_length = int(length * percent)
    bar = '█' * filled_length + '░' * (length - filled_length)
    return f"[{bar}] {int(percent * 100)}%"

def format_date(iso_date_str):
    """
    Converts ISO date string to a friendly format.
    Example: 2024-03-01T00:00:00 -> 01 Mar 2024
    """
    try:
        if not iso_date_str: return "N/A"
        # Handle cases with or without time components
        if "T" in iso_date_str:
            dt = datetime.fromisoformat(iso_date_str)
        else:
            dt = datetime.strptime(iso_date_str, "%Y-%m-%d")
            
        return dt.strftime("%d %b %Y")
    except:
        return iso_date_str

def get_greeting():
    """Returns a time-based greeting."""
    hour = datetime.now().hour
    if 5 <= hour < 12:
        return "Good Morning ☀️"
    elif 12 <= hour < 17:
        return "Good Afternoon 🌤"
    elif 17 <= hour < 21:
        return "Good Evening 🌆"
    else:
        return "Hello 👋"

def get_random_quote():
    """Returns a random educational quote."""
    quotes = [
        "“Education is the most powerful weapon which you can use to change the world.” – Nelson Mandela",
        "“The beautiful thing about learning is that no one can take it away from you.” – B.B. King",
        "“Live as if you were to die tomorrow. Learn as if you were to live forever.” – Mahatma Gandhi",
        "“Education is not the filling of a pail, but the lighting of a fire.” – W.B. Yeats",
        "“The roots of education are bitter, but the fruit is sweet.” – Aristotle"
    ]
    return random.choice(quotes)

def format_currency(amount):
    """
    Formats a number as currency.
    Example: 50000 -> ₹50,000
    """
    try:
        if amount is None: return "₹0"
        return f"₹{float(amount):,.0f}"
    except:
        return f"₹{amount}"

def get_footer():
    """Returns the standard bot footer."""
    return "\n━━━━━━━━━━━━━━━━━━━━\n_Bot Created by Rahul | CMS v1.0_"

def get_role_badge(role):
    """Returns an emoji badge for a given role."""
    badges = {
        "Admin": "👑",
        "Teacher": "👨‍🏫",
        "Student": "👨‍🎓"
    }
    return badges.get(role, "👤")

def get_status_emoji(is_active):
    """Returns a status emoji."""
    return "✅" if is_active else "❌"

# --- HTML Formatting Helpers ---
def html_bold(text):
    return f"<b>{text}</b>"

def html_italic(text):
    return f"<i>{text}</i>"

def html_code(text):
    return f"<code>{text}</code>"

def html_pre(text, language=""):
    return f"<pre><code class='language-{language}'>{text}</code></pre>"

def html_underline(text):
    return f"<u>{text}</u>"

def html_strikethrough(text):
    return f"<s>{text}</s>"

def html_spoiler(text):
    return f"<tg-spoiler>{text}</tg-spoiler>"

def html_quote(text):
    return f"<blockquote>{text}</blockquote>"

def html_expandable_quote(text):
    return f"<blockquote expandable>{text}</blockquote>"

def html_link(text, url):
    return f"<a href='{url}'>{text}</a>"

def html_mention(name, user_id):
    return f"<a href='tg://user?id={user_id}'>{name}</a>"

def esc(text):
    """Escapes HTML special characters."""
    if not text: return ""
    return str(text).replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
