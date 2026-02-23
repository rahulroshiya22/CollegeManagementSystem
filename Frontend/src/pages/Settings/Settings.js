import React, { useState } from 'react';
import { useSelector } from 'react-redux';
import {
  Bell,
  Shield,
  Palette,
  Globe,
  Database,
  HelpCircle,
  Download,
  Trash2,
  Moon,
  Sun,
  Volume2,
  Wifi,
  Smartphone,
} from 'lucide-react';
import toast from 'react-hot-toast';

const Settings = () => {
  const { user } = useSelector((state) => state.auth);
  
  const [settings, setSettings] = useState({
    // Notifications
    emailNotifications: true,
    pushNotifications: true,
    smsNotifications: false,
    emailDigest: 'daily',
    
    // Appearance
    theme: 'light',
    language: 'english',
    fontSize: 'medium',
    sidebarCollapsed: false,
    
    // Privacy
    twoFactorAuth: false,
    profileVisibility: 'public',
    showOnlineStatus: true,
    
    // System
    autoSave: true,
    dataSync: true,
    cacheEnabled: true,
    
    // Sound
    notificationSound: true,
    alertVolume: 50,
  });

  const handleSettingChange = (category, setting, value) => {
    setSettings(prev => ({
      ...prev,
      [category]: {
        ...prev[category],
        [setting]: value
      }
    }));
  };

  const handleSimpleChange = (setting, value) => {
    setSettings(prev => ({
      ...prev,
      [setting]: value
    }));
  };

  const handleSave = () => {
    // Save settings to backend
    toast.success('Settings saved successfully');
  };

  const handleExportData = () => {
    // Export user data
    toast.success('Data export started');
  };

  const handleDeleteAccount = () => {
    // Show confirmation modal
    toast.error('Account deletion requires additional verification');
  };

  const SettingSection = ({ title, icon: Icon, children }) => (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <div className="flex items-center space-x-3 mb-6">
        <div className="p-2 bg-primary-100 rounded-lg">
          <Icon className="w-5 h-5 text-primary-600" />
        </div>
        <h3 className="text-lg font-semibold text-gray-900">{title}</h3>
      </div>
      {children}
    </div>
  );

  const ToggleSetting = ({ label, description, value, onChange }) => (
    <div className="flex items-center justify-between py-3 border-b border-gray-100 last:border-0">
      <div className="flex-1">
        <p className="text-sm font-medium text-gray-900">{label}</p>
        {description && (
          <p className="text-xs text-gray-500 mt-1">{description}</p>
        )}
      </div>
      <button
        onClick={() => onChange(!value)}
        className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
          value ? 'bg-primary-600' : 'bg-gray-200'
        }`}
      >
        <span
          className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
            value ? 'translate-x-6' : 'translate-x-1'
          }`}
        />
      </button>
    </div>
  );

  const SelectSetting = ({ label, value, options, onChange }) => (
    <div className="flex items-center justify-between py-3 border-b border-gray-100 last:border-0">
      <p className="text-sm font-medium text-gray-900">{label}</p>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="input text-sm w-32"
      >
        {options.map(option => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </div>
  );

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Settings</h1>
          <p className="text-gray-600">Manage your account settings and preferences</p>
        </div>
        <button
          onClick={handleSave}
          className="btn btn-primary btn-md"
        >
          Save Changes
        </button>
      </div>

      {/* Notifications Settings */}
      <SettingSection title="Notifications" icon={Bell}>
        <ToggleSetting
          label="Email Notifications"
          description="Receive email updates about your account activity"
          value={settings.emailNotifications}
          onChange={(value) => handleSimpleChange('emailNotifications', value)}
        />
        <ToggleSetting
          label="Push Notifications"
          description="Receive push notifications in your browser"
          value={settings.pushNotifications}
          onChange={(value) => handleSimpleChange('pushNotifications', value)}
        />
        <ToggleSetting
          label="SMS Notifications"
          description="Receive text messages for important updates"
          value={settings.smsNotifications}
          onChange={(value) => handleSimpleChange('smsNotifications', value)}
        />
        <SelectSetting
          label="Email Digest"
          value={settings.emailDigest}
          options={[
            { value: 'daily', label: 'Daily' },
            { value: 'weekly', label: 'Weekly' },
            { value: 'monthly', label: 'Monthly' },
            { value: 'never', label: 'Never' },
          ]}
          onChange={(value) => handleSimpleChange('emailDigest', value)}
        />
      </SettingSection>

      {/* Appearance Settings */}
      <SettingSection title="Appearance" icon={Palette}>
        <div className="flex items-center justify-between py-3 border-b border-gray-100">
          <p className="text-sm font-medium text-gray-900">Theme</p>
          <div className="flex items-center space-x-2">
            <button
              onClick={() => handleSimpleChange('theme', 'light')}
              className={`p-2 rounded-lg ${
                settings.theme === 'light' ? 'bg-primary-100 text-primary-600' : 'text-gray-400'
              }`}
            >
              <Sun className="w-4 h-4" />
            </button>
            <button
              onClick={() => handleSimpleChange('theme', 'dark')}
              className={`p-2 rounded-lg ${
                settings.theme === 'dark' ? 'bg-primary-100 text-primary-600' : 'text-gray-400'
              }`}
            >
              <Moon className="w-4 h-4" />
            </button>
          </div>
        </div>
        <SelectSetting
          label="Language"
          value={settings.language}
          options={[
            { value: 'english', label: 'English' },
            { value: 'spanish', label: 'Spanish' },
            { value: 'french', label: 'French' },
            { value: 'german', label: 'German' },
          ]}
          onChange={(value) => handleSimpleChange('language', value)}
        />
        <SelectSetting
          label="Font Size"
          value={settings.fontSize}
          options={[
            { value: 'small', label: 'Small' },
            { value: 'medium', label: 'Medium' },
            { value: 'large', label: 'Large' },
          ]}
          onChange={(value) => handleSimpleChange('fontSize', value)}
        />
        <ToggleSetting
          label="Collapsed Sidebar"
          description="Keep sidebar collapsed by default"
          value={settings.sidebarCollapsed}
          onChange={(value) => handleSimpleChange('sidebarCollapsed', value)}
        />
      </SettingSection>

      {/* Privacy Settings */}
      <SettingSection title="Privacy & Security" icon={Shield}>
        <ToggleSetting
          label="Two-Factor Authentication"
          description="Add an extra layer of security to your account"
          value={settings.twoFactorAuth}
          onChange={(value) => handleSimpleChange('twoFactorAuth', value)}
        />
        <SelectSetting
          label="Profile Visibility"
          value={settings.profileVisibility}
          options={[
            { value: 'public', label: 'Public' },
            { value: 'students', label: 'Students Only' },
            { value: 'private', label: 'Private' },
          ]}
          onChange={(value) => handleSimpleChange('profileVisibility', value)}
        />
        <ToggleSetting
          label="Show Online Status"
          description="Let others see when you're online"
          value={settings.showOnlineStatus}
          onChange={(value) => handleSimpleChange('showOnlineStatus', value)}
        />
      </SettingSection>

      {/* System Settings */}
      <SettingSection title="System" icon={Database}>
        <ToggleSetting
          label="Auto-Save"
          description="Automatically save your work"
          value={settings.autoSave}
          onChange={(value) => handleSimpleChange('autoSave', value)}
        />
        <ToggleSetting
          label="Data Sync"
          description="Sync your data across devices"
          value={settings.dataSync}
          onChange={(value) => handleSimpleChange('dataSync', value)}
        />
        <ToggleSetting
          label="Cache Enabled"
          description="Enable caching for better performance"
          value={settings.cacheEnabled}
          onChange={(value) => handleSimpleChange('cacheEnabled', value)}
        />
      </SettingSection>

      {/* Sound Settings */}
      <SettingSection title="Sound" icon={Volume2}>
        <ToggleSetting
          label="Notification Sound"
          description="Play sound for notifications"
          value={settings.notificationSound}
          onChange={(value) => handleSimpleChange('notificationSound', value)}
        />
        <div className="py-3 border-b border-gray-100">
          <p className="text-sm font-medium text-gray-900 mb-3">Alert Volume</p>
          <input
            type="range"
            min="0"
            max="100"
            value={settings.alertVolume}
            onChange={(e) => handleSimpleChange('alertVolume', parseInt(e.target.value))}
            className="w-full"
          />
          <div className="flex justify-between text-xs text-gray-500 mt-1">
            <span>0%</span>
            <span>{settings.alertVolume}%</span>
            <span>100%</span>
          </div>
        </div>
      </SettingSection>

      {/* Data Management */}
      <SettingSection title="Data Management" icon={Database}>
        <div className="space-y-4">
          <button
            onClick={handleExportData}
            className="w-full flex items-center justify-between p-4 border border-gray-200 rounded-lg hover:bg-gray-50"
          >
            <div className="flex items-center space-x-3">
              <Download className="w-5 h-5 text-gray-400" />
              <div className="text-left">
                <p className="text-sm font-medium text-gray-900">Export Data</p>
                <p className="text-xs text-gray-500">Download all your data</p>
              </div>
            </div>
            <span className="text-gray-400">→</span>
          </button>
          
          <button
            onClick={handleDeleteAccount}
            className="w-full flex items-center justify-between p-4 border border-red-200 rounded-lg hover:bg-red-50"
          >
            <div className="flex items-center space-x-3">
              <Trash2 className="w-5 h-5 text-red-500" />
              <div className="text-left">
                <p className="text-sm font-medium text-red-600">Delete Account</p>
                <p className="text-xs text-red-500">Permanently delete your account</p>
              </div>
            </div>
            <span className="text-red-400">→</span>
          </button>
        </div>
      </SettingSection>

      {/* Help & Support */}
      <SettingSection title="Help & Support" icon={HelpCircle}>
        <div className="space-y-4">
          <button className="w-full flex items-center justify-between p-4 border border-gray-200 rounded-lg hover:bg-gray-50">
            <div className="flex items-center space-x-3">
              <HelpCircle className="w-5 h-5 text-gray-400" />
              <div className="text-left">
                <p className="text-sm font-medium text-gray-900">Help Center</p>
                <p className="text-xs text-gray-500">Get help with common issues</p>
              </div>
            </div>
            <span className="text-gray-400">→</span>
          </button>
          
          <button className="w-full flex items-center justify-between p-4 border border-gray-200 rounded-lg hover:bg-gray-50">
            <div className="flex items-center space-x-3">
              <Globe className="w-5 h-5 text-gray-400" />
              <div className="text-left">
                <p className="text-sm font-medium text-gray-900">Documentation</p>
                <p className="text-xs text-gray-500">View detailed documentation</p>
              </div>
            </div>
            <span className="text-gray-400">→</span>
          </button>
        </div>
      </SettingSection>
    </div>
  );
};

export default Settings;
