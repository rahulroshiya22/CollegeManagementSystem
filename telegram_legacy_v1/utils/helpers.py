from services.api_client import api_client

# Cache validation (simplistic)
_course_cache = {}

def get_course_name_map():
    """Returns a dictionary {courseId: courseTitle}."""
    global _course_cache
    if _course_cache:
        return _course_cache
        
    response = api_client.get("/api/course")
    if "error" in response:
        return {}
        
    data = response if isinstance(response, list) else response.get("data", [])
    
    for c in data:
        _course_cache[c.get('courseId')] = c.get('courseName')
        
    return _course_cache

def get_course_name(course_id):
    mapping = get_course_name_map()
    return mapping.get(int(course_id), f"Unknown Course ({course_id})")

# Cache for departments
_dept_cache = {}

def get_department_name_map():
    """Returns a dictionary {deptId: deptName}."""
    global _dept_cache
    if _dept_cache:
        return _dept_cache
        
    response = api_client.get("/api/department")
    if "error" in response:
        return {}
        
    data = response if isinstance(response, list) else response.get("data", [])
    
    for d in data:
        _dept_cache[d.get('departmentId')] = d.get('name')
        
    return _dept_cache

def get_department_name(dept_id):
    mapping = get_department_name_map()
    return mapping.get(int(dept_id), f"Unknown Dept ({dept_id})")

async def edit_response(query, text, reply_markup):
    """Smartly edits the message, handling both Photo (Caption) and Text messages."""
    try:
        if query.message.photo:
            # It's a photo message, edit the caption
            # Caption limit is 1024 chars.
            if len(text) > 1000:
                # Too long for caption, must switch to text message
                await query.message.delete()
                await query.message.reply_text(text, reply_markup=reply_markup, parse_mode="Markdown")
            else:
                await query.edit_message_caption(caption=text, reply_markup=reply_markup, parse_mode="Markdown")
        else:
            # It's a text message, edit the text
            await query.edit_message_text(text=text, reply_markup=reply_markup, parse_mode="Markdown")
    except Exception as e:
        if "Message is not modified" in str(e):
             return # Ignore
        # Fallback: If type mismatch or other error, try sending new
        try:
             await query.message.reply_text(text, reply_markup=reply_markup, parse_mode="Markdown")
        except:
             pass
